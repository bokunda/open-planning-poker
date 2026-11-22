using OpenPlanningPoker.GameEngine.Domain.Teams;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Teams;

public sealed class OrganizationMemberInfo
{
    public Guid Id { get; set; }
    public OrganizationRole Role { get; set; }

    public async Task<string?> GetNameAsync([Service] IUserService users, CancellationToken cancellationToken) =>
        (await users.GetAsync(Id.ToString(), cancellationToken))?.UserName;
}

public sealed class OrganizationTeamInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
}

public sealed class TeamComparisonInfo
{
    public Guid TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sprints { get; set; }
    public double? FirstRoundConsensusShare { get; set; }
    public double? ReVotesPerTicket { get; set; }
    public double? AverageVelocity { get; set; }
    public List<double> Velocity { get; set; } = [];
}

/// <summary>An organization as the caller sees it (OPP-154). Members see the names of its teams; admins also its numbers.</summary>
public sealed class OrganizationInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>The caller's role; null when the caller has not joined.</summary>
    public OrganizationRole? MyRole { get; set; }

    /// <summary>The secret of the invite link. Admins only.</summary>
    public string? InviteToken { get; set; }

    public async Task<List<OrganizationMemberInfo>> GetMembersAsync([Service] OrganizationService service, CancellationToken cancellationToken) =>
        MyRole is null ? [] : [.. (await service.MembersAsync(Id, cancellationToken)).Select(m => new OrganizationMemberInfo { Id = m.PlayerId, Role = m.Role })];

    public async Task<List<OrganizationTeamInfo>> GetTeamsAsync([Service] OrganizationService service, CancellationToken cancellationToken) =>
        MyRole is null ? [] : [.. (await service.TeamsAsync(Id, cancellationToken)).Select(t => new OrganizationTeamInfo { Id = t.TeamId, Name = t.Name, OwnerId = t.OwnerId })];

    /// <summary>First-vote agreement, re-votes and velocity of every team side by side (OPP-157). Admins only.</summary>
    public async Task<List<TeamComparisonInfo>> GetComparisonAsync([Service] OrganizationService service, CancellationToken cancellationToken) =>
        MyRole != OrganizationRole.Admin ? [] : [.. (await service.CompareAsync(Id, cancellationToken)).Select(c => new TeamComparisonInfo
        {
            TeamId = c.TeamId, Name = c.Name, Sprints = c.Sprints, FirstRoundConsensusShare = c.FirstRoundConsensusShare,
            ReVotesPerTicket = c.ReVotesPerTicket, AverageVelocity = c.AverageVelocity, Velocity = [.. c.Velocity],
        })];

    public static OrganizationInfo From(Organization o, OrganizationMember? me) => new()
    {
        Id = o.Id, Name = o.Name, MyRole = me?.Role, InviteToken = me?.Role == OrganizationRole.Admin ? o.InviteToken : null,
    };
}

[ExtendObjectType(typeof(Team))]
public sealed class TeamOrganizationFields
{
    /// <summary>The organization the team belongs to (OPP-154); null for a team on its own or for non-members.</summary>
    public async Task<OrganizationTeamLink?> GetOrganizationAsync(
        [Parent] Team team, [Service] IGameAccessService gameAccess, [Service] ITeamRepository teams, [Service] OrganizationService service,
        CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureTeamMemberAsync(team.Id, cancellationToken) is not null) return null;
        var entity = await teams.GetByIdAsync(team.Id, cancellationToken);
        if (entity?.OrganizationId is not { } id || await service.GetAsync(id, cancellationToken) is not { } organization) return null;
        return new OrganizationTeamLink { Id = organization.Id, Name = organization.Name };
    }
}

public sealed class OrganizationTeamLink
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[QueryType]
public class OrganizationQueries
{
    /// <summary>An organization by id. Anyone with the link sees its name; members see more.</summary>
    public async Task<FieldResult<OrganizationInfo, ApplicationError>> GetOrganizationAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service, [Required] Guid id, CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } authError) return authError;
        var organization = await service.GetAsync(id, cancellationToken);
        if (organization is null) return OrganizationErrors.NotFound;
        return OrganizationInfo.From(organization, await service.MemberAsync(id, gameAccess.CurrentUserId, cancellationToken));
    }

    /// <summary>Organizations the caller belongs to.</summary>
    public async Task<List<OrganizationInfo>> GetMyOrganizationsAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service, CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is not null) return [];
        var result = new List<OrganizationInfo>();
        foreach (var organization in await service.MineAsync(gameAccess.CurrentUserId, cancellationToken))
        {
            result.Add(OrganizationInfo.From(organization, await service.MemberAsync(organization.Id, gameAccess.CurrentUserId, cancellationToken)));
        }
        return result;
    }
}

[MutationType]
public class OrganizationMutations
{
    public async Task<FieldResult<OrganizationInfo, ApplicationError>> CreateOrganizationAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service, [Required] string name, CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } authError) return authError;
        var result = await service.CreateAsync(name, gameAccess.CurrentUserId, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        return OrganizationInfo.From(result.Value!, await service.MemberAsync(result.Value!.Id, gameAccess.CurrentUserId, cancellationToken));
    }

    public async Task<FieldResult<bool, ApplicationError>> RenameOrganizationAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service, [Required] Guid id, [Required] string name, CancellationToken cancellationToken = default)
    {
        if (await AdminAsync(gameAccess, service, id, cancellationToken) is { } error) return error;
        var result = await service.RenameAsync(id, name, cancellationToken);
        return result.IsSuccess ? true : result.Error!;
    }

    /// <summary>Joins with the token from the invite link.</summary>
    public async Task<FieldResult<bool, ApplicationError>> JoinOrganizationAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service, [Required] Guid id, [Required] string token, CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } authError) return authError;
        var result = await service.JoinAsync(id, token, gameAccess.CurrentUserId, cancellationToken);
        return result.IsSuccess ? true : result.Error!;
    }

    /// <summary>A new invite link; the old one stops working. Admins.</summary>
    public async Task<FieldResult<OrganizationInfo, ApplicationError>> RotateOrganizationInviteAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service, [Required] Guid id, CancellationToken cancellationToken = default)
    {
        if (await AdminAsync(gameAccess, service, id, cancellationToken) is { } error) return error;
        var result = await service.RotateInviteAsync(id, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        return OrganizationInfo.From(result.Value!, await service.MemberAsync(id, gameAccess.CurrentUserId, cancellationToken));
    }

    public async Task<FieldResult<bool, ApplicationError>> SetOrganizationRoleAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service,
        [Required] Guid id, [Required] Guid playerId, [Required] OrganizationRole role, CancellationToken cancellationToken = default)
    {
        if (await AdminAsync(gameAccess, service, id, cancellationToken) is { } error) return error;
        var result = await service.SetRoleAsync(id, playerId, role, cancellationToken);
        return result.IsSuccess ? true : result.Error!;
    }

    /// <summary>Leaves the organization (your own id) or removes a member (admins).</summary>
    public async Task<FieldResult<bool, ApplicationError>> RemoveOrganizationMemberAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service,
        [Required] Guid id, [Required] Guid playerId, CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } authError) return authError;
        if (playerId != gameAccess.CurrentUserId && await AdminAsync(gameAccess, service, id, cancellationToken) is { } error) return error;
        var result = await service.RemoveMemberAsync(id, playerId, cancellationToken);
        return result.IsSuccess ? true : result.Error!;
    }

    /// <summary>Moves your own team into the organization; you keep it.</summary>
    public async Task<FieldResult<bool, ApplicationError>> AddTeamToOrganizationAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service,
        [Required] Guid id, [Required] Guid teamId, CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } authError) return authError;
        var result = await service.AddTeamAsync(id, teamId, gameAccess.CurrentUserId, cancellationToken);
        return result.IsSuccess ? true : result.Error!;
    }

    /// <summary>Takes a team out of the organization: its owner or an admin.</summary>
    public async Task<FieldResult<bool, ApplicationError>> RemoveTeamFromOrganizationAsync(
        [Service] IGameAccessService gameAccess, [Service] OrganizationService service,
        [Required] Guid id, [Required] Guid teamId, CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } authError) return authError;
        var result = await service.RemoveTeamAsync(id, teamId, gameAccess.CurrentUserId, cancellationToken);
        return result.IsSuccess ? true : result.Error!;
    }

    private static async Task<ApplicationError?> AdminAsync(IGameAccessService gameAccess, OrganizationService service, Guid id, CancellationToken cancellationToken)
    {
        if (gameAccess.EnsureAuthenticated() is { } authError) return authError;
        if (await service.GetAsync(id, cancellationToken) is null) return OrganizationErrors.NotFound;
        var me = await service.MemberAsync(id, gameAccess.CurrentUserId, cancellationToken);
        return me?.Role == OrganizationRole.Admin ? null : OrganizationErrors.NotAdmin;
    }
}
