using System.Globalization;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;
using OpenPlanningPoker.GameEngine.Application.Features.Tickets;

namespace OpenPlanningPoker.GameEngine.Application.Features.Teams;

/// <summary>Days one member is available in the sprint (OPP-146).</summary>
public sealed record MemberCapacity(Guid PlayerId, double Days, bool IsDefault);

/// <summary>
/// Suggested points for the sprint (OPP-147). With capacity in past sprints: points per person-day of those sprints
/// times this sprint's person-days. Otherwise the average points of the last sprints.
/// </summary>
public sealed record PointBudget(bool EnoughHistory, bool BasedOnCapacity, int SprintsUsed, int? Suggested, int? Low, int? High, double? PointsPerPersonDay);

/// <summary>One estimated (or not yet estimated) ticket of the sprint plan (OPP-148).</summary>
public sealed record PlanTicket(Guid TicketId, string Name, string? ExternalKey, string? ExternalUrl, string? Estimate, double? Points, bool Planned);

public sealed record SprintPlanResponse(
    Guid GameId,
    int SprintDays,
    bool SprintDaysSet,
    IReadOnlyList<MemberCapacity> Members,
    double PersonDays,
    PointBudget Budget,
    double PlannedPoints,
    IReadOnlyList<PlanTicket> Tickets);

public sealed record JiraSprintMoveResult(int Moved, int Skipped);

public sealed record GetSprintPlanQuery(Guid TeamId, Guid GameId) : IRequest<Result<SprintPlanResponse, ApplicationError>>;
public sealed record SetSprintDaysCommand(Guid TeamId, Guid GameId, int Days) : IRequest<Result<SprintPlanResponse, ApplicationError>>;
public sealed record SetMemberCapacityCommand(Guid TeamId, Guid GameId, Guid PlayerId, double Days) : IRequest<Result<SprintPlanResponse, ApplicationError>>;
public sealed record SetTicketPlannedCommand(Guid TeamId, Guid TicketId, bool Planned) : IRequest<Result<SprintPlanResponse, ApplicationError>>;
public sealed record MovePlanToJiraSprintCommand(Guid TeamId, Guid GameId, long SprintId, Guid PlayerId) : IRequest<Result<JiraSprintMoveResult, ApplicationError>>;

public static class PlanningErrors
{
    public static ApplicationError SprintNotInTeam => new("SprintNotInTeam", "This planning session does not belong to the team.");
    public static ApplicationError InvalidSprintDays => new("InvalidSprintDays", $"A sprint has 1 to {Game.MaxSprintDays} working days.");
    public static ApplicationError InvalidMemberDays => new("InvalidMemberDays", "Use 0 to the sprint length, in half days.");
    public static ApplicationError NotAMember => new("NotATeamMember", "This player is not a member of the team.");
    public static ApplicationError NothingToMove => new("NothingPlanned", "No planned ticket is linked to Jira.");
}

public static class Planning
{
    /// <summary>Sprints the budget looks back on.</summary>
    public const int LookBack = 3;

    /// <summary>Sprints needed before a budget is suggested.</summary>
    public const int MinHistory = 2;

    /// <summary>Estimate of a ticket as points; null when not estimated or not a number (e.g. T-shirt sizes).</summary>
    public static double? PointsOf(string? estimate) =>
        double.TryParse(estimate, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : null;

    /// <summary>Past sprint input of the budget: its points and, when it had capacity, its person-days.</summary>
    public sealed record PastSprint(double Points, double? PersonDays);

    /// <summary>Person-days of a sprint: every member's days, or the sprint length for members without a row.</summary>
    public static double PersonDays(int sprintDays, IEnumerable<Guid> members, IReadOnlyCollection<SprintCapacity> capacities) =>
        members.Sum(player => Math.Min(sprintDays, capacities.FirstOrDefault(c => c.PlayerId == player)?.Days ?? sprintDays));

    /// <summary>The suggestion of OPP-147 from the past sprints (newest first) and this sprint's person-days.</summary>
    public static PointBudget Budget(IReadOnlyList<PastSprint> past, double personDays)
    {
        var withCapacity = past.Where(s => s.PersonDays is > 0).Take(LookBack).ToList();
        if (withCapacity.Count >= MinHistory)
        {
            var rate = withCapacity.Sum(s => s.Points) / withCapacity.Sum(s => s.PersonDays!.Value);
            var rates = withCapacity.Select(s => s.Points / s.PersonDays!.Value).ToList();
            return new PointBudget(true, true, withCapacity.Count,
                (int)Math.Floor(rate * personDays), (int)Math.Floor(rates.Min() * personDays), (int)Math.Floor(rates.Max() * personDays),
                Math.Round(rate, 2));
        }

        var recent = past.Take(LookBack).ToList();
        if (recent.Count < MinHistory)
        {
            return new PointBudget(false, false, recent.Count, null, null, null, null);
        }

        return new PointBudget(true, false, recent.Count,
            (int)Math.Floor(recent.Average(s => s.Points)), (int)Math.Floor(recent.Min(s => s.Points)), (int)Math.Floor(recent.Max(s => s.Points)), null);
    }

    private static bool IsJira(Ticket ticket) =>
        ticket.ExternalKey is not null && (ticket.ExternalProvider is null or "jira")
        && ticket.ExternalUrl is { } url && LinkTicket.IsJiraIssueUrl(url);

    /// <summary>Loads everything the plan of one team sprint needs.</summary>
    public sealed class Loader(IGameRepository games, ITeamRepository teams, ITicketRepository tickets, IVoteRepository votes)
    {
        public async Task<Result<Game, ApplicationError>> SprintAsync(Guid teamId, Guid gameId, CancellationToken cancellationToken)
        {
            var game = await games.GetByIdAsync(gameId, cancellationToken);
            return game is null || game.TeamId != teamId ? PlanningErrors.SprintNotInTeam : game;
        }

        public async Task<SprintPlanResponse> BuildAsync(Game game, CancellationToken cancellationToken)
        {
            var teamId = game.TeamId!.Value;
            var members = (await teams.GetMembersAsync(teamId, cancellationToken)).Select(m => m.PlayerId).ToList();
            var sprints = (await games.GetByTeamAsync(teamId, cancellationToken))
                .Where(g => g.Id != game.Id && g.CreatedOn < game.CreatedOn)
                .ToList();
            var capacities = await teams.GetCapacitiesAsync([game.Id, .. sprints.Select(s => s.Id)], cancellationToken);

            var past = new List<PastSprint>();
            foreach (var sprint in sprints)
            {
                var (points, estimated) = await PointsAsync(sprint.Id, cancellationToken);
                if (estimated == 0) continue;
                var own = capacities.Where(c => c.GameId == sprint.Id).ToList();
                double? pastDays = sprint.SprintDays is not null || own.Count > 0
                    ? PersonDays(sprint.SprintDays ?? Game.DefaultSprintDays, members, own)
                    : null;
                past.Add(new PastSprint(points, pastDays));
            }

            var sprintDays = game.SprintDays ?? Game.DefaultSprintDays;
            var mine = capacities.Where(c => c.GameId == game.Id).ToList();
            var memberDays = members.Select(player => mine.FirstOrDefault(c => c.PlayerId == player) is { } row
                ? new MemberCapacity(player, Math.Min(sprintDays, row.Days), false)
                : new MemberCapacity(player, sprintDays, true)).ToList();
            var personDays = memberDays.Sum(m => m.Days);

            var gameVotes = await votes.GetByGame(game.Id, null, cancellationToken);
            var planTickets = (await tickets.GetByGame(game.Id, cancellationToken)).Select(ticket =>
            {
                var estimate = ticket.IsEstimated
                    ? Teams.EstimateOf(ticket, [.. gameVotes.Where(v => v.TicketId == ticket.Id).Select(v => v.Value)])
                    : null;
                return new PlanTicket(ticket.Id, ticket.Name, ticket.ExternalKey, ticket.ExternalUrl, estimate, PointsOf(estimate), ticket.Planned);
            }).ToList();

            return new SprintPlanResponse(game.Id, sprintDays, game.SprintDays is not null, memberDays, personDays,
                Budget(past, personDays), planTickets.Where(t => t.Planned).Sum(t => t.Points ?? 0), planTickets);
        }

        private async Task<(double Points, int Estimated)> PointsAsync(Guid gameId, CancellationToken cancellationToken)
        {
            var gameVotes = await votes.GetByGame(gameId, null, cancellationToken);
            var estimates = (await tickets.GetByGame(gameId, cancellationToken))
                .Where(t => t.IsEstimated)
                .Select(t => Teams.EstimateOf(t, [.. gameVotes.Where(v => v.TicketId == t.Id).Select(v => v.Value)]))
                .Where(e => e is not null)
                .ToList();
            return (estimates.Sum(e => PointsOf(e) ?? 0), estimates.Count);
        }
    }

    public sealed class GetHandler(Loader loader) : IRequestHandler<GetSprintPlanQuery, Result<SprintPlanResponse, ApplicationError>>
    {
        public async Task<Result<SprintPlanResponse, ApplicationError>> Handle(GetSprintPlanQuery request, CancellationToken cancellationToken)
        {
            var sprint = await loader.SprintAsync(request.TeamId, request.GameId, cancellationToken);
            return sprint.IsSuccess ? await loader.BuildAsync(sprint.Value!, cancellationToken) : sprint.Error!;
        }
    }

    public sealed class SprintDaysHandler(Loader loader, IUnitOfWork unitOfWork)
        : IRequestHandler<SetSprintDaysCommand, Result<SprintPlanResponse, ApplicationError>>
    {
        public async Task<Result<SprintPlanResponse, ApplicationError>> Handle(SetSprintDaysCommand request, CancellationToken cancellationToken)
        {
            if (request.Days is < 1 or > Game.MaxSprintDays)
            {
                return PlanningErrors.InvalidSprintDays;
            }

            var sprint = await loader.SprintAsync(request.TeamId, request.GameId, cancellationToken);
            if (!sprint.IsSuccess) return sprint.Error!;

            sprint.Value!.SetSprintDays(request.Days);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return await loader.BuildAsync(sprint.Value, cancellationToken);
        }
    }

    public sealed class MemberDaysHandler(Loader loader, ITeamRepository teams, IUnitOfWork unitOfWork)
        : IRequestHandler<SetMemberCapacityCommand, Result<SprintPlanResponse, ApplicationError>>
    {
        public async Task<Result<SprintPlanResponse, ApplicationError>> Handle(SetMemberCapacityCommand request, CancellationToken cancellationToken)
        {
            var sprint = await loader.SprintAsync(request.TeamId, request.GameId, cancellationToken);
            if (!sprint.IsSuccess) return sprint.Error!;
            var game = sprint.Value!;

            var sprintDays = game.SprintDays ?? Game.DefaultSprintDays;
            if (request.Days < 0 || request.Days > sprintDays || Math.Abs(request.Days * 2 - Math.Round(request.Days * 2)) > 1e-9)
            {
                return PlanningErrors.InvalidMemberDays;
            }

            if ((await teams.GetMembersAsync(request.TeamId, cancellationToken)).All(m => m.PlayerId != request.PlayerId))
            {
                return PlanningErrors.NotAMember;
            }

            var row = (await teams.GetCapacitiesAsync([game.Id], cancellationToken)).FirstOrDefault(c => c.PlayerId == request.PlayerId);
            if (row is null)
            {
                teams.AddCapacity(SprintCapacity.Create(game.Id, request.PlayerId, request.Days));
            }
            else
            {
                row.SetDays(request.Days);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return await loader.BuildAsync(game, cancellationToken);
        }
    }

    public sealed class PlannedHandler(Loader loader, ITicketRepository tickets, IUnitOfWork unitOfWork)
        : IRequestHandler<SetTicketPlannedCommand, Result<SprintPlanResponse, ApplicationError>>
    {
        public async Task<Result<SprintPlanResponse, ApplicationError>> Handle(SetTicketPlannedCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null) return GameAccessErrors.TicketNotFound;

            var sprint = await loader.SprintAsync(request.TeamId, ticket.GameId, cancellationToken);
            if (!sprint.IsSuccess) return sprint.Error!;

            ticket.SetPlanned(request.Planned);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return await loader.BuildAsync(sprint.Value!, cancellationToken);
        }
    }

    public sealed class JiraSprintHandler(Loader loader, ITicketRepository tickets, JiraService jira)
        : IRequestHandler<MovePlanToJiraSprintCommand, Result<JiraSprintMoveResult, ApplicationError>>
    {
        public async Task<Result<JiraSprintMoveResult, ApplicationError>> Handle(MovePlanToJiraSprintCommand request, CancellationToken cancellationToken)
        {
            var sprint = await loader.SprintAsync(request.TeamId, request.GameId, cancellationToken);
            if (!sprint.IsSuccess) return sprint.Error!;

            var planned = (await tickets.GetByGame(request.GameId, cancellationToken)).Where(t => t.Planned).ToList();
            var keys = planned.Where(IsJira).Select(t => t.ExternalKey!).Distinct().ToList();
            if (keys.Count == 0) return PlanningErrors.NothingToMove;

            var moved = await jira.MoveToSprintAsync(request.PlayerId, request.SprintId, keys, cancellationToken);
            return moved.IsSuccess ? new JiraSprintMoveResult(moved.Value, planned.Count - keys.Count) : moved.Error!;
        }
    }
}
