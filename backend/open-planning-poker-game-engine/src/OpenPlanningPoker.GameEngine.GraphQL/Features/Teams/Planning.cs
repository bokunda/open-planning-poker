namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Teams;

/// <summary>Days one member is available in the sprint (OPP-146).</summary>
public sealed class MemberCapacityInfo
{
    public Guid PlayerId { get; set; }
    public double Days { get; set; }

    /// <summary>Nobody entered days for this member, so the whole sprint is assumed.</summary>
    public bool IsDefault { get; set; }
}

/// <summary>Suggested points for the sprint (OPP-147). A planning aid, not a target.</summary>
public sealed class PointBudgetInfo
{
    /// <summary>False with fewer than two past sprints: no suggestion yet.</summary>
    public bool EnoughHistory { get; set; }

    /// <summary>Computed from points per person-day of past sprints with capacity; otherwise their average points.</summary>
    public bool BasedOnCapacity { get; set; }
    public int SprintsUsed { get; set; }
    public int? Suggested { get; set; }
    public int? Low { get; set; }
    public int? High { get; set; }
    public double? PointsPerPersonDay { get; set; }
}

public sealed class PlanTicketInfo
{
    public Guid TicketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ExternalKey { get; set; }
    public string? ExternalUrl { get; set; }

    /// <summary>Final estimate, or the most common card; null while not estimated.</summary>
    public string? Estimate { get; set; }

    /// <summary>The estimate as a number; null for non-numeric cards.</summary>
    public double? Points { get; set; }
    public bool Planned { get; set; }
}

/// <summary>Capacity, budget and the chosen tickets of one team sprint (OPP-138).</summary>
public sealed class SprintPlan
{
    public Guid GameId { get; set; }
    public int SprintDays { get; set; }

    /// <summary>The team entered the sprint length (otherwise the default of 10 days).</summary>
    public bool SprintDaysSet { get; set; }
    public List<MemberCapacityInfo> Members { get; set; } = [];
    public double PersonDays { get; set; }
    public PointBudgetInfo Budget { get; set; } = new();
    public double PlannedPoints { get; set; }
    public List<PlanTicketInfo> Tickets { get; set; } = [];

    public static SprintPlan From(SprintPlanResponse r) => new()
    {
        GameId = r.GameId,
        SprintDays = r.SprintDays,
        SprintDaysSet = r.SprintDaysSet,
        Members = [.. r.Members.Select(m => new MemberCapacityInfo { PlayerId = m.PlayerId, Days = m.Days, IsDefault = m.IsDefault })],
        PersonDays = r.PersonDays,
        Budget = new PointBudgetInfo
        {
            EnoughHistory = r.Budget.EnoughHistory,
            BasedOnCapacity = r.Budget.BasedOnCapacity,
            SprintsUsed = r.Budget.SprintsUsed,
            Suggested = r.Budget.Suggested,
            Low = r.Budget.Low,
            High = r.Budget.High,
            PointsPerPersonDay = r.Budget.PointsPerPersonDay,
        },
        PlannedPoints = r.PlannedPoints,
        Tickets = [.. r.Tickets.Select(t => new PlanTicketInfo
        {
            TicketId = t.TicketId,
            Name = t.Name,
            ExternalKey = t.ExternalKey,
            ExternalUrl = t.ExternalUrl,
            Estimate = t.Estimate,
            Points = t.Points,
            Planned = t.Planned,
        })],
    };
}

/// <summary>Result of moving the planned tickets into a Jira sprint (OPP-149).</summary>
public sealed class JiraSprintMoved
{
    public int Moved { get; set; }

    /// <summary>Planned tickets that are not Jira issues.</summary>
    public int Skipped { get; set; }
}

[QueryType]
public class PlanningQueries
{
    /// <summary>Capacity, budget and plan of one sprint of a team. Members only.</summary>
    public async Task<FieldResult<SprintPlan, ApplicationError>> GetSprintPlanAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamMemberAsync(teamId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new GetSprintPlanQuery(teamId, gameId), cancellationToken);
        return result.IsSuccess ? SprintPlan.From(result.Value!) : result.Error!;
    }
}

[MutationType]
public class PlanningMutations
{
    /// <summary>Sets the working days of the sprint (1–30). Team owner only.</summary>
    public async Task<FieldResult<SprintPlan, ApplicationError>> SetSprintDaysAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        [Required] Guid gameId,
        [Required] int days,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new SetSprintDaysCommand(teamId, gameId, days), cancellationToken);
        return result.IsSuccess ? SprintPlan.From(result.Value!) : result.Error!;
    }

    /// <summary>Sets how many days a member is available (in half days). The owner for anyone, members for themselves.</summary>
    public async Task<FieldResult<SprintPlan, ApplicationError>> SetMemberCapacityAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        [Required] Guid gameId,
        [Required] Guid playerId,
        [Required] double days,
        CancellationToken cancellationToken = default)
    {
        var accessError = playerId == gameAccess.CurrentUserId
            ? await gameAccess.EnsureTeamMemberAsync(teamId, cancellationToken)
            : await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        var result = await sender.Send(new SetMemberCapacityCommand(teamId, gameId, playerId, days), cancellationToken);
        return result.IsSuccess ? SprintPlan.From(result.Value!) : result.Error!;
    }

    /// <summary>Puts a ticket into the sprint plan or takes it out. Team owner only.</summary>
    public async Task<FieldResult<SprintPlan, ApplicationError>> SetTicketPlannedAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        [Required] Guid ticketId,
        [Required] bool planned,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new SetTicketPlannedCommand(teamId, ticketId, planned), cancellationToken);
        return result.IsSuccess ? SprintPlan.From(result.Value!) : result.Error!;
    }

    /// <summary>
    /// Moves the planned Jira tickets into a Jira sprint with the caller's Jira connection. Other tickets are skipped.
    /// Team owner only.
    /// </summary>
    public async Task<FieldResult<JiraSprintMoved, ApplicationError>> MovePlanToJiraSprintAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        [Required] Guid gameId,
        [Required] long sprintId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new MovePlanToJiraSprintCommand(teamId, gameId, sprintId, gameAccess.CurrentUserId), cancellationToken);
        return result.IsSuccess
            ? new JiraSprintMoved { Moved = result.Value!.Moved, Skipped = result.Value.Skipped }
            : result.Error!;
    }
}
