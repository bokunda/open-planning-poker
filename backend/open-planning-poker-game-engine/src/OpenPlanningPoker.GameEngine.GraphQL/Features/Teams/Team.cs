namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Teams;

/// <summary>
/// A persistent team room. Anyone with the link sees its name and can join; members also see
/// the other members, the sprint history and the velocity.
/// </summary>
public sealed class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>True when the caller is a member (or the owner).</summary>
    public async Task<bool> GetIsMemberAsync([Service] IGameAccessService gameAccess, CancellationToken cancellationToken) =>
        await gameAccess.EnsureTeamMemberAsync(Id, cancellationToken) is null;

    /// <summary>Members of the team, oldest first. Empty for non-members.</summary>
    public async Task<List<TeamMember>> GetMembersAsync(
        [Service] IGameAccessService gameAccess,
        [Service] ISender sender,
        CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureTeamMemberAsync(Id, cancellationToken) is not null)
        {
            return [];
        }

        var ids = await sender.Send(new GetTeamMembersQuery(Id), cancellationToken);
        return [.. ids.Select(id => new TeamMember { Id = id })];
    }

    /// <summary>The team's planning sessions, newest first, with their points. Empty for non-members.</summary>
    public async Task<List<Sprint>> GetSprintsAsync(
        [Service] IGameAccessService gameAccess,
        [Service] ISender sender,
        CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureTeamMemberAsync(Id, cancellationToken) is not null)
        {
            return [];
        }

        var sprints = await sender.Send(new GetTeamSprintsQuery(Id), cancellationToken);
        return [.. sprints.Select(Sprint.From)];
    }

    /// <summary>
    /// Average points of the last three sprints that estimated something; null until there is one.
    /// A planning aid, not a target.
    /// </summary>
    public async Task<double?> GetAverageVelocityAsync(
        [Service] IGameAccessService gameAccess,
        [Service] ISender sender,
        CancellationToken cancellationToken)
    {
        var recent = (await GetSprintsAsync(gameAccess, sender, cancellationToken))
            .Where(sprint => sprint.EstimatedCount > 0)
            .Take(3)
            .ToList();
        return recent.Count == 0 ? null : Math.Round(recent.Average(sprint => sprint.Points), 1);
    }

    public static Team From(TeamResponse team) => new()
    {
        Id = team.Id,
        Name = team.Name,
        Description = team.Description,
        OwnerId = team.OwnerId,
        CreatedOn = team.CreatedOn
    };
}

public sealed class TeamMember
{
    public Guid Id { get; set; }

    public async Task<string?> GetNameAsync([Service] IUserService userService, CancellationToken cancellationToken = default) =>
        (await userService.GetAsync(Id.ToString(), cancellationToken))?.UserName;
}

/// <summary>One planning session of a team. Points add up the estimates of its revealed tickets.</summary>
public sealed class Sprint
{
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public int TicketCount { get; set; }
    public int EstimatedCount { get; set; }
    public double Points { get; set; }

    public static Sprint From(SprintSummary sprint) => new()
    {
        GameId = sprint.GameId,
        Name = sprint.Name,
        Description = sprint.Description,
        CreatedOn = sprint.CreatedOn,
        TicketCount = sprint.TicketCount,
        EstimatedCount = sprint.EstimatedCount,
        Points = sprint.Points
    };
}

[QueryType]
public class TeamQueries
{
    /// <summary>A team room by id. Members-only fields are empty for others.</summary>
    public async Task<FieldResult<Team, ApplicationError>> GetTeamAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new GetTeamQuery(id), cancellationToken);
        return result.IsSuccess ? Team.From(result.Value!) : result.Error!;
    }

    /// <summary>Team rooms the caller belongs to.</summary>
    public async Task<List<Team>> GetMyTeamsAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is not null)
        {
            return [];
        }

        var teams = await sender.Send(new GetMyTeamsQuery(gameAccess.CurrentUserId), cancellationToken);
        return [.. teams.Select(Team.From)];
    }
}

[MutationType]
public class TeamMutations
{
    /// <summary>Creates a team room; the caller becomes its owner and first member.</summary>
    public async Task<FieldResult<Team, ApplicationError>> CreateTeamAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError)
        {
            return accessError;
        }

        if (rateLimiter.TryAcquire(ActionRateLimiter.CreateGame, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        var result = await sender.Send(new CreateTeamCommand(name, description ?? string.Empty, gameAccess.CurrentUserId), cancellationToken);
        return result.IsSuccess ? Team.From(result.Value!) : result.Error!;
    }

    /// <summary>Renames a team room. Owner only.</summary>
    public async Task<FieldResult<Team, ApplicationError>> UpdateTeamAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        [Required] string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new UpdateTeamCommand(teamId, name, description ?? string.Empty), cancellationToken);
        return result.IsSuccess ? Team.From(result.Value!) : result.Error!;
    }

    /// <summary>Joins a team room with its link.</summary>
    public async Task<FieldResult<bool, ApplicationError>> JoinTeamAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError)
        {
            return accessError;
        }

        if (rateLimiter.TryAcquire(ActionRateLimiter.JoinGame, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        var result = await sender.Send(new JoinTeamCommand(teamId, gameAccess.CurrentUserId), cancellationToken);
        return result.IsSuccess ? result.Value : result.Error!;
    }

    /// <summary>Leaves a team room. The owner cannot leave.</summary>
    public async Task<FieldResult<bool, ApplicationError>> LeaveTeamAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new LeaveTeamCommand(teamId, gameAccess.CurrentUserId), cancellationToken);
        return result.IsSuccess ? result.Value : result.Error!;
    }
}
