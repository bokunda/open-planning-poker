using System.Globalization;
using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;

namespace OpenPlanningPoker.GameEngine.Application.Features.Teams;

public sealed record TeamResponse(Guid Id, string Name, string Description, Guid OwnerId, DateTimeOffset CreatedOn);

/// <summary>One planning session (game) of a team, with the points it estimated.</summary>
public sealed record SprintSummary(
    Guid GameId,
    string Name,
    string Description,
    DateTimeOffset CreatedOn,
    int TicketCount,
    int EstimatedCount,
    double Points,
    SessionStats? Stats = null);

/// <summary>How a planning session went (OPP-134). Team numbers only, never per player.</summary>
public sealed record SessionStats(int Revealed, int FirstRoundConsensus, int ReVotes, int DiscussedCount, double DiscussionSeconds, int NeedsInfo)
{
    /// <summary>Share (0–1) of revealed tickets agreed on the first vote; null without revealed tickets.</summary>
    public double? FirstRoundConsensusShare => Revealed == 0 ? null : Math.Round((double)FirstRoundConsensus / Revealed, 2);

    /// <summary>Extra voting rounds per revealed ticket.</summary>
    public double? ReVotesPerTicket => Revealed == 0 ? null : Math.Round((double)ReVotes / Revealed, 2);

    /// <summary>Average seconds from the reveal to the final estimate.</summary>
    public double? AverageDiscussionSeconds => DiscussedCount == 0 ? null : Math.Round(DiscussionSeconds / DiscussedCount);
}

public sealed record CreateTeamCommand(string Name, string Description, Guid OwnerId) : IRequest<Result<TeamResponse, ApplicationError>>;
public sealed record UpdateTeamCommand(Guid TeamId, string Name, string Description) : IRequest<Result<TeamResponse, ApplicationError>>;
public sealed record JoinTeamCommand(Guid TeamId, Guid PlayerId) : IRequest<Result<bool, ApplicationError>>;
public sealed record LeaveTeamCommand(Guid TeamId, Guid PlayerId) : IRequest<Result<bool, ApplicationError>>;
public sealed record GetTeamQuery(Guid TeamId) : IRequest<Result<TeamResponse, ApplicationError>>;
public sealed record GetTeamMembersQuery(Guid TeamId) : IRequest<IReadOnlyList<Guid>>;
public sealed record GetMyTeamsQuery(Guid PlayerId) : IRequest<IReadOnlyList<TeamResponse>>;
public sealed record GetTeamSprintsQuery(Guid TeamId) : IRequest<IReadOnlyList<SprintSummary>>;

public static class Teams
{
    public sealed class CreateValidator : AbstractValidator<CreateTeamCommand>
    {
        public CreateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Description).MaximumLength(4080);
            RuleFor(x => x.OwnerId).NotEmpty();
        }
    }

    public sealed class UpdateValidator : AbstractValidator<UpdateTeamCommand>
    {
        public UpdateValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Description).MaximumLength(4080);
        }
    }

    public sealed class CreateHandler(ITeamRepository teams, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateTeamCommand, Result<TeamResponse, ApplicationError>>
    {
        public async Task<Result<TeamResponse, ApplicationError>> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
        {
            var team = Team.Create(request.Name.Trim(), request.Description.Trim());
            teams.Add(team);
            teams.AddMember(TeamMember.Create(team.Id, request.OwnerId));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ToResponse(team);
        }
    }

    public sealed class UpdateHandler(ITeamRepository teams, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateTeamCommand, Result<TeamResponse, ApplicationError>>
    {
        public async Task<Result<TeamResponse, ApplicationError>> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await teams.GetByIdAsync(request.TeamId, cancellationToken);
            if (team is null)
            {
                return GameAccessErrors.TeamNotFound;
            }

            team.Update(request.Name.Trim(), request.Description.Trim());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ToResponse(team);
        }
    }

    public sealed class JoinHandler(ITeamRepository teams, IUnitOfWork unitOfWork)
        : IRequestHandler<JoinTeamCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(JoinTeamCommand request, CancellationToken cancellationToken)
        {
            if (await teams.GetByIdAsync(request.TeamId, cancellationToken) is null)
            {
                return GameAccessErrors.TeamNotFound;
            }

            var members = await teams.GetMembersAsync(request.TeamId, cancellationToken);
            if (members.Any(member => member.PlayerId == request.PlayerId))
            {
                return true;
            }

            teams.AddMember(TeamMember.Create(request.TeamId, request.PlayerId));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public sealed class LeaveHandler(ITeamRepository teams, IUnitOfWork unitOfWork)
        : IRequestHandler<LeaveTeamCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(LeaveTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await teams.GetByIdAsync(request.TeamId, cancellationToken);
            if (team is null)
            {
                return GameAccessErrors.TeamNotFound;
            }

            if (team.CreatedBy == request.PlayerId)
            {
                return new ApplicationError("TeamOwnerCannotLeave", "The team owner cannot leave the team.");
            }

            var member = (await teams.GetMembersAsync(request.TeamId, cancellationToken))
                .FirstOrDefault(item => item.PlayerId == request.PlayerId);
            if (member is not null)
            {
                teams.RemoveMember(member);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }

    public sealed class GetHandler(ITeamRepository teams)
        : IRequestHandler<GetTeamQuery, Result<TeamResponse, ApplicationError>>
    {
        public async Task<Result<TeamResponse, ApplicationError>> Handle(GetTeamQuery request, CancellationToken cancellationToken)
        {
            var team = await teams.GetByIdAsync(request.TeamId, cancellationToken);
            return team is null ? GameAccessErrors.TeamNotFound : ToResponse(team);
        }
    }

    public sealed class MembersHandler(ITeamRepository teams) : IRequestHandler<GetTeamMembersQuery, IReadOnlyList<Guid>>
    {
        public async Task<IReadOnlyList<Guid>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken) =>
            [.. (await teams.GetMembersAsync(request.TeamId, cancellationToken)).Select(member => member.PlayerId)];
    }

    public sealed class MyTeamsHandler(ITeamRepository teams) : IRequestHandler<GetMyTeamsQuery, IReadOnlyList<TeamResponse>>
    {
        public async Task<IReadOnlyList<TeamResponse>> Handle(GetMyTeamsQuery request, CancellationToken cancellationToken) =>
            [.. (await teams.GetByMemberAsync(request.PlayerId, cancellationToken)).Select(ToResponse)];
    }

    public sealed class SprintsHandler(IGameRepository games, ITicketRepository tickets, IVoteRepository votes)
        : IRequestHandler<GetTeamSprintsQuery, IReadOnlyList<SprintSummary>>
    {
        public async Task<IReadOnlyList<SprintSummary>> Handle(GetTeamSprintsQuery request, CancellationToken cancellationToken)
        {
            var sprints = await games.GetByTeamAsync(request.TeamId, cancellationToken);
            var result = new List<SprintSummary>(sprints.Count);

            foreach (var game in sprints)
            {
                var gameTickets = (await tickets.GetByGame(game.Id, cancellationToken)).ToList();
                var gameVotes = await votes.GetByGame(game.Id, null, cancellationToken);
                var estimates = gameTickets
                    .Where(ticket => ticket.IsEstimated)
                    .Select(ticket => EstimateOf(ticket, gameVotes.Where(vote => vote.TicketId == ticket.Id).Select(vote => vote.Value).ToList()))
                    .Where(estimate => estimate is not null)
                    .ToList();
                var points = estimates
                    .Select(estimate => double.TryParse(estimate, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : 0)
                    .Sum();

                result.Add(new SprintSummary(game.Id, game.Name, game.Description, game.CreatedOn, gameTickets.Count, estimates.Count, points,
                    StatsOf(gameTickets, gameVotes)));
            }

            return result;
        }
    }

    /// <summary>Consensus on the first vote, re-votes, discussion time and needs-info tickets of one session (OPP-134).</summary>
    public static SessionStats StatsOf(IReadOnlyCollection<Ticket> gameTickets, IEnumerable<Vote> gameVotes)
    {
        var revealed = gameTickets.Where(t => t.VotesRevealed).ToList();
        var byTicket = gameVotes.GroupBy(v => v.TicketId).ToDictionary(g => g.Key, g => g.Select(v => v.Value).ToList());
        var consensus = revealed.Count(t => t.VoteRounds <= 1
            && TicketStats.From(true, byTicket.GetValueOrDefault(t.Id) ?? []).Spread is VoteSpread.Consensus or VoteSpread.Close);
        var reVotes = revealed.Sum(t => Math.Max(0, t.VoteRounds - 1));
        var discussed = revealed.Where(t => t.RevealedAt is not null && t.EstimatedAt is not null && t.EstimatedAt >= t.RevealedAt).ToList();
        var seconds = discussed.Sum(t => (t.EstimatedAt!.Value - t.RevealedAt!.Value).TotalSeconds);
        return new SessionStats(revealed.Count, consensus, reVotes, discussed.Count, seconds, gameTickets.Count(t => t.NeedsInfo));
    }

    /// <summary>The host's final estimate, otherwise the most common card (as in the report).</summary>
    public static string? EstimateOf(Ticket ticket, IReadOnlyCollection<string> voteValues) =>
        ticket.FinalEstimate ?? TicketStats.From(ticket.VotesRevealed, voteValues).Estimate;

    private static TeamResponse ToResponse(Team team) =>
        new(team.Id, team.Name, team.Description, team.CreatedBy, team.CreatedOn);
}
