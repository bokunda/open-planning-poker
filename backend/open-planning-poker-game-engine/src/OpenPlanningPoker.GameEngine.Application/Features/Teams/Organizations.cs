using System.Security.Cryptography;

namespace OpenPlanningPoker.GameEngine.Application.Features.Teams;

public sealed record OrganizationMemberItem(Guid PlayerId, OrganizationRole Role);

public sealed record OrganizationTeamItem(Guid TeamId, string Name, Guid OwnerId);

/// <summary>Session numbers of one team, side by side with the others (OPP-157). Team numbers only, never per player.</summary>
public sealed record TeamComparison(
    Guid TeamId, string Name, int Sprints, double? FirstRoundConsensusShare, double? ReVotesPerTicket, double? AverageVelocity,
    IReadOnlyList<double> Velocity);

public static class OrganizationErrors
{
    public static ApplicationError NotFound => new("OrganizationNotFound", "Organization not found.");
    public static ApplicationError InvalidName => new("InvalidOrganizationName", $"Give the organization a name of 1 to {Organization.MaxNameLength} characters.");
    public static ApplicationError NotMember => new("NotOrganizationMember", "Join the organization first.");
    public static ApplicationError NotAdmin => new("NotOrganizationAdmin", "Only an admin of the organization can do this.");
    public static ApplicationError InvalidInvite => new("InvalidOrganizationInvite", "This invite link is no longer valid. Ask an admin for a new one.");
    public static ApplicationError LastAdmin => new("LastOrganizationAdmin", "The organization needs at least one admin.");
    public static ApplicationError NotTeamOwner => new("NotTeamOwner", "Only the team owner can move the team.");
    public static ApplicationError TeamNotInOrganization => new("TeamNotInOrganization", "This team is not in the organization.");
}

/// <summary>Organizations with team rooms and roles (OPP-154) and the numbers of their teams (OPP-157).</summary>
public sealed class OrganizationService(IOrganizationRepository organizations, ITeamRepository teams, ISender sender, IUnitOfWork unitOfWork, IAuditLog auditLog)
{
    /// <summary>Sprints compared per team.</summary>
    public const int LookBack = 6;

    public static string NewInviteToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public Task<Organization?> GetAsync(Guid id, CancellationToken cancellationToken) => organizations.GetAsync(id, cancellationToken);

    public Task<IReadOnlyList<Organization>> MineAsync(Guid playerId, CancellationToken cancellationToken) => organizations.GetByMemberAsync(playerId, cancellationToken);

    public Task<OrganizationMember?> MemberAsync(Guid id, Guid playerId, CancellationToken cancellationToken) => organizations.GetMemberAsync(id, playerId, cancellationToken);

    public async Task<IReadOnlyList<OrganizationMemberItem>> MembersAsync(Guid id, CancellationToken cancellationToken) =>
        [.. (await organizations.GetMembersAsync(id, cancellationToken)).Select(m => new OrganizationMemberItem(m.PlayerId, m.Role))];

    public async Task<IReadOnlyList<OrganizationTeamItem>> TeamsAsync(Guid id, CancellationToken cancellationToken) =>
        [.. (await organizations.GetTeamsAsync(id, cancellationToken)).Select(t => new OrganizationTeamItem(t.Id, t.Name, t.CreatedBy))];

    public async Task<Result<Organization, ApplicationError>> CreateAsync(string name, Guid creatorId, CancellationToken cancellationToken)
    {
        if (!ValidName(name)) return OrganizationErrors.InvalidName;
        var organization = Organization.Create(name, NewInviteToken());
        organizations.Add(organization);
        organizations.AddMember(OrganizationMember.Create(organization.Id, creatorId, OrganizationRole.Admin));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return organization;
    }

    public async Task<Result<Organization, ApplicationError>> RenameAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        if (!ValidName(name)) return OrganizationErrors.InvalidName;
        var organization = await organizations.GetAsync(id, cancellationToken);
        if (organization is null) return OrganizationErrors.NotFound;
        organization.Rename(name);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Organization(AuditActions.OrganizationRenamed, AuditType.Update, id, detail: organization.Name), cancellationToken);
        return organization;
    }

    /// <summary>Joins with the invite link; joining again changes nothing.</summary>
    public async Task<Result<bool, ApplicationError>> JoinAsync(Guid id, string token, Guid playerId, CancellationToken cancellationToken)
    {
        var organization = await organizations.GetAsync(id, cancellationToken);
        if (organization is null) return OrganizationErrors.NotFound;
        if (!CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(organization.InviteToken), System.Text.Encoding.UTF8.GetBytes(token ?? string.Empty)))
        {
            return OrganizationErrors.InvalidInvite;
        }

        if (await organizations.GetMemberAsync(id, playerId, cancellationToken) is null)
        {
            organizations.AddMember(OrganizationMember.Create(id, playerId, OrganizationRole.Member));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await auditLog.RecordAsync(AuditEntry.Organization(AuditActions.OrganizationMemberJoined, AuditType.Create, id, playerId), cancellationToken);
        }
        return true;
    }

    public async Task<Result<Organization, ApplicationError>> RotateInviteAsync(Guid id, CancellationToken cancellationToken)
    {
        var organization = await organizations.GetAsync(id, cancellationToken);
        if (organization is null) return OrganizationErrors.NotFound;
        organization.SetInviteToken(NewInviteToken());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Organization(AuditActions.OrganizationInviteRotated, AuditType.Update, id), cancellationToken);
        return organization;
    }

    public async Task<Result<bool, ApplicationError>> SetRoleAsync(Guid id, Guid playerId, OrganizationRole role, CancellationToken cancellationToken)
    {
        var members = await organizations.GetMembersAsync(id, cancellationToken);
        var member = members.FirstOrDefault(m => m.PlayerId == playerId);
        if (member is null) return OrganizationErrors.NotMember;
        if (member.Role == OrganizationRole.Admin && role != OrganizationRole.Admin && members.Count(m => m.Role == OrganizationRole.Admin) == 1)
        {
            return OrganizationErrors.LastAdmin;
        }
        member.SetRole(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Organization(AuditActions.OrganizationMemberRole, AuditType.Update, id, playerId, value: role.ToString()), cancellationToken);
        return true;
    }

    /// <summary>A member leaves, or an admin removes them. The last admin cannot go.</summary>
    public async Task<Result<bool, ApplicationError>> RemoveMemberAsync(Guid id, Guid playerId, CancellationToken cancellationToken)
    {
        var members = await organizations.GetMembersAsync(id, cancellationToken);
        var member = members.FirstOrDefault(m => m.PlayerId == playerId);
        if (member is null) return OrganizationErrors.NotMember;
        if (member.Role == OrganizationRole.Admin && members.Count(m => m.Role == OrganizationRole.Admin) == 1) return OrganizationErrors.LastAdmin;
        organizations.RemoveMember(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Organization(AuditActions.OrganizationMemberRemoved, AuditType.Delete, id, playerId), cancellationToken);
        return true;
    }

    /// <summary>The team's owner moves it in, being a member of the organization; the team stays theirs.</summary>
    public async Task<Result<bool, ApplicationError>> AddTeamAsync(Guid id, Guid teamId, Guid callerId, CancellationToken cancellationToken)
    {
        if (await organizations.GetMemberAsync(id, callerId, cancellationToken) is null) return OrganizationErrors.NotMember;
        var team = await teams.GetByIdAsync(teamId, cancellationToken);
        if (team is null) return new ApplicationError("TeamNotFound", "Team not found.");
        if (team.CreatedBy != callerId) return OrganizationErrors.NotTeamOwner;
        team.MoveToOrganization(id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Organization(AuditActions.OrganizationTeamAdded, AuditType.Create, id, teamId, team.Name), cancellationToken);
        return true;
    }

    /// <summary>The team's owner or an admin takes the team out; the team itself is kept.</summary>
    public async Task<Result<bool, ApplicationError>> RemoveTeamAsync(Guid id, Guid teamId, Guid callerId, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(teamId, cancellationToken);
        if (team is null || team.OrganizationId != id) return OrganizationErrors.TeamNotInOrganization;
        var caller = await organizations.GetMemberAsync(id, callerId, cancellationToken);
        if (team.CreatedBy != callerId && caller?.Role != OrganizationRole.Admin) return OrganizationErrors.NotAdmin;
        team.MoveToOrganization(null);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Organization(AuditActions.OrganizationTeamRemoved, AuditType.Delete, id, teamId, team.Name), cancellationToken);
        return true;
    }

    /// <summary>The numbers of every team over its last <see cref="LookBack"/> sessions (OPP-157).</summary>
    public async Task<IReadOnlyList<TeamComparison>> CompareAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = new List<TeamComparison>();
        foreach (var team in await organizations.GetTeamsAsync(id, cancellationToken))
        {
            var sprints = (await sender.Send(new GetTeamSprintsQuery(team.Id), cancellationToken))
                .OrderByDescending(s => s.CreatedOn).Take(LookBack).ToList();
            result.Add(Compare(team.Id, team.Name, sprints));
        }
        return result;
    }

    /// <summary>Averages over the sessions that have numbers; velocity oldest first.</summary>
    public static TeamComparison Compare(Guid teamId, string name, IReadOnlyList<SprintSummary> newestFirst)
    {
        static double? Average(IEnumerable<double?> values)
        {
            var list = values.Where(v => v is not null).Select(v => v!.Value).ToList();
            return list.Count == 0 ? null : Math.Round(list.Average(), 2);
        }

        var estimated = newestFirst.Where(s => s.EstimatedCount > 0).ToList();
        return new TeamComparison(teamId, name, newestFirst.Count,
            Average(newestFirst.Select(s => s.Stats?.FirstRoundConsensusShare)),
            Average(newestFirst.Select(s => s.Stats?.ReVotesPerTicket)),
            estimated.Count == 0 ? null : Math.Round(estimated.Average(s => s.Points), 1),
            [.. estimated.AsEnumerable().Reverse().Select(s => s.Points)]);
    }

    private static bool ValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= Organization.MaxNameLength;
}
