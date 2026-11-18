using System.Globalization;
using System.Text.RegularExpressions;
using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;

namespace OpenPlanningPoker.GameEngine.Application.Features.Teams;

/// <summary>An estimated ticket of one of the team's sprints (OPP-129).</summary>
public sealed record TeamTicket(
    Guid TicketId,
    Guid GameId,
    string SprintName,
    DateTimeOffset SprintDate,
    string Name,
    string Description,
    string? ExternalKey,
    string? ExternalUrl,
    string Estimate,
    string Spread);

/// <summary>A pinned reference ticket with the card value it stands for (OPP-127).</summary>
public sealed record TeamAnchorResponse(Guid Id, string Value, string? Note, TeamTicket Ticket);

public sealed record TeamHistoryPage(IReadOnlyList<TeamTicket> Items, int TotalCount);

/// <summary>What the Reference panel shows next to the cards (OPP-128).</summary>
public sealed record TicketReferenceResponse(Guid? TeamId, IReadOnlyList<TeamAnchorResponse> Anchors, IReadOnlyList<TeamTicket> Similar);

public sealed record GetTeamAnchorsQuery(Guid TeamId) : IRequest<IReadOnlyList<TeamAnchorResponse>>;
public sealed record PinAnchorCommand(Guid TeamId, Guid TicketId, string? Note) : IRequest<Result<TeamAnchorResponse, ApplicationError>>;
public sealed record UnpinAnchorCommand(Guid TeamId, Guid AnchorId) : IRequest<Result<bool, ApplicationError>>;
public sealed record SearchTeamHistoryQuery(Guid TeamId, string? Search, string? Value, int Skip, int Take) : IRequest<TeamHistoryPage>;
public sealed record GetTicketReferenceQuery(Guid TicketId) : IRequest<Result<TicketReferenceResponse, ApplicationError>>;

public static class ReferenceErrors
{
    public static ApplicationError NotInTeam => new("TicketNotInTeam", "This ticket is not part of the team's sprints.");
    public static ApplicationError NotEstimated => new("TicketNotEstimated", "Only estimated tickets can be pinned.");
    public static ApplicationError AnchorLimit => new("AnchorLimit", "This card already has two reference tickets. Unpin one first.");
    public static ApplicationError AnchorNotFound => new("AnchorNotFound", "Reference ticket not found.");
}

public static partial class TeamReference
{
    public const int MaxPageSize = 50;
    public const int SimilarCount = 3;

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex Words();

    /// <summary>Words of a title used for "similar tickets": lower case, three letters or more, without the issue key.</summary>
    public static HashSet<string> Tokens(string text, string? key = null)
    {
        var words = Words().Matches(text.ToLowerInvariant()).Select(m => m.Value).Where(w => w.Length >= 3);
        var keyParts = key is null ? [] : Words().Matches(key.ToLowerInvariant()).Select(m => m.Value).ToHashSet();
        return [.. words.Where(w => !keyParts.Contains(w))];
    }

    /// <summary>Share of common words (Jaccard), 0 when nothing is shared.</summary>
    public static double Similarity(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var common = a.Count(b.Contains);
        return common == 0 ? 0 : (double)common / (a.Count + b.Count - common);
    }

    /// <summary>Every estimated ticket of the team's sprints, newest sprint first.</summary>
    public static async Task<IReadOnlyList<TeamTicket>> EstimatedTicketsAsync(
        Guid teamId, IGameRepository games, ITicketRepository tickets, IVoteRepository votes, CancellationToken cancellationToken)
    {
        var sprints = await games.GetByTeamAsync(teamId, cancellationToken);
        var result = new List<TeamTicket>();
        foreach (var game in sprints)
        {
            var gameVotes = await votes.GetByGame(game.Id, null, cancellationToken);
            foreach (var ticket in await tickets.GetByGame(game.Id, cancellationToken))
            {
                if (!ticket.IsEstimated) continue;
                var values = gameVotes.Where(v => v.TicketId == ticket.Id).Select(v => v.Value).ToList();
                var stats = TicketStats.From(true, values);
                var estimate = ticket.FinalEstimate ?? stats.Estimate;
                if (string.IsNullOrWhiteSpace(estimate)) continue;
                result.Add(new TeamTicket(ticket.Id, game.Id, game.Name, game.CreatedOn, ticket.Name, ticket.Description,
                    ticket.ExternalKey, ticket.ExternalUrl, estimate, stats.Spread.ToString()));
            }
        }

        return result;
    }

    private static List<TeamAnchorResponse> Join(IEnumerable<TeamAnchor> anchors, IReadOnlyList<TeamTicket> teamTickets) =>
        [.. anchors
            .Select(anchor => (anchor, ticket: teamTickets.FirstOrDefault(t => t.TicketId == anchor.TicketId)))
            .Where(pair => pair.ticket is not null)
            .Select(pair => new TeamAnchorResponse(pair.anchor.Id, pair.anchor.Value, pair.anchor.Note, pair.ticket!))];

    public sealed class AnchorsHandler(ITeamRepository teams, IGameRepository games, ITicketRepository tickets, IVoteRepository votes)
        : IRequestHandler<GetTeamAnchorsQuery, IReadOnlyList<TeamAnchorResponse>>
    {
        public async Task<IReadOnlyList<TeamAnchorResponse>> Handle(GetTeamAnchorsQuery request, CancellationToken cancellationToken)
        {
            var anchors = await teams.GetAnchorsAsync(request.TeamId, cancellationToken);
            if (anchors.Count == 0) return [];
            return Join(anchors, await EstimatedTicketsAsync(request.TeamId, games, tickets, votes, cancellationToken));
        }
    }

    public sealed class PinValidator : AbstractValidator<PinAnchorCommand>
    {
        public PinValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.Note).MaximumLength(TeamAnchor.MaxNoteLength);
        }
    }

    public sealed class PinHandler(ITeamRepository teams, IGameRepository games, ITicketRepository tickets, IVoteRepository votes, IUnitOfWork unitOfWork)
        : IRequestHandler<PinAnchorCommand, Result<TeamAnchorResponse, ApplicationError>>
    {
        public async Task<Result<TeamAnchorResponse, ApplicationError>> Handle(PinAnchorCommand request, CancellationToken cancellationToken)
        {
            var teamTickets = await EstimatedTicketsAsync(request.TeamId, games, tickets, votes, cancellationToken);
            var ticket = teamTickets.FirstOrDefault(t => t.TicketId == request.TicketId);
            if (ticket is null)
            {
                var exists = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
                var game = exists is null ? null : await games.GetByIdAsync(exists.GameId, cancellationToken);
                return game?.TeamId == request.TeamId ? ReferenceErrors.NotEstimated : ReferenceErrors.NotInTeam;
            }

            var anchors = await teams.GetAnchorsAsync(request.TeamId, cancellationToken);
            var existing = anchors.FirstOrDefault(a => a.TicketId == request.TicketId);
            if (existing is not null)
            {
                teams.RemoveAnchor(existing);
            }
            else if (anchors.Count(a => a.Value == ticket.Estimate) >= TeamAnchor.MaxPerValue)
            {
                return ReferenceErrors.AnchorLimit;
            }

            var anchor = TeamAnchor.Create(request.TeamId, request.TicketId, ticket.Estimate, request.Note);
            teams.AddAnchor(anchor);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new TeamAnchorResponse(anchor.Id, anchor.Value, anchor.Note, ticket);
        }
    }

    public sealed class UnpinHandler(ITeamRepository teams, IUnitOfWork unitOfWork)
        : IRequestHandler<UnpinAnchorCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(UnpinAnchorCommand request, CancellationToken cancellationToken)
        {
            var anchor = (await teams.GetAnchorsAsync(request.TeamId, cancellationToken)).FirstOrDefault(a => a.Id == request.AnchorId);
            if (anchor is null)
            {
                return ReferenceErrors.AnchorNotFound;
            }

            teams.RemoveAnchor(anchor);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public sealed class HistoryHandler(IGameRepository games, ITicketRepository tickets, IVoteRepository votes)
        : IRequestHandler<SearchTeamHistoryQuery, TeamHistoryPage>
    {
        public async Task<TeamHistoryPage> Handle(SearchTeamHistoryQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<TeamTicket> items = await EstimatedTicketsAsync(request.TeamId, games, tickets, votes, cancellationToken);
            var search = request.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                items = items.Where(t =>
                    t.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || t.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (t.ExternalKey?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(request.Value))
            {
                items = items.Where(t => t.Estimate == request.Value.Trim());
            }

            var list = items.ToList();
            var take = Math.Clamp(request.Take, 1, MaxPageSize);
            return new TeamHistoryPage([.. list.Skip(Math.Max(0, request.Skip)).Take(take)], list.Count);
        }
    }

    public sealed class TicketReferenceHandler(ITeamRepository teams, IGameRepository games, ITicketRepository tickets, IVoteRepository votes)
        : IRequestHandler<GetTicketReferenceQuery, Result<TicketReferenceResponse, ApplicationError>>
    {
        public async Task<Result<TicketReferenceResponse, ApplicationError>> Handle(GetTicketReferenceQuery request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            var game = await games.GetByIdAsync(ticket.GameId, cancellationToken);
            if (game?.TeamId is not { } teamId)
            {
                return new TicketReferenceResponse(null, [], []);
            }

            var teamTickets = await EstimatedTicketsAsync(teamId, games, tickets, votes, cancellationToken);
            var anchors = Join(await teams.GetAnchorsAsync(teamId, cancellationToken), teamTickets)
                .OrderBy(a => CardOrder(a.Value)).ToList();

            var words = Tokens(ticket.Name, ticket.ExternalKey);
            var similar = teamTickets
                .Where(t => t.TicketId != ticket.Id)
                .Select(t => (ticket: t, score: Similarity(words, Tokens(t.Name, t.ExternalKey))))
                .Where(pair => pair.score > 0)
                .OrderByDescending(pair => pair.score)
                .ThenByDescending(pair => pair.ticket.SprintDate)
                .Take(SimilarCount)
                .Select(pair => pair.ticket)
                .ToList();

            return new TicketReferenceResponse(teamId, anchors, similar);
        }
    }

    /// <summary>Numeric cards in order, others (T-shirt sizes, ?) after them in text order.</summary>
    private static (int, double, string) CardOrder(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? (0, n, value) : (1, 0, value);
}
