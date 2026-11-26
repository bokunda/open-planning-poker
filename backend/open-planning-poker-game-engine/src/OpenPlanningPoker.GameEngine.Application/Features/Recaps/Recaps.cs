using System.Globalization;
using System.Security.Cryptography;
using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using OpenPlanningPoker.Shared.Services;

namespace OpenPlanningPoker.GameEngine.Application.Features.Recaps;

/// <summary>Whether the recap of a game is shared, and its link token (OPP-170).</summary>
public sealed record RecapSharingResponse(bool Shared, string? Token, bool ShowNames);

/// <param name="DiscussionMinutes">From turning the cards over to choosing the estimate; null when unknown.</param>
public sealed record RecapTicketItem(string Name, string? ExternalKey, string? Estimate, VoteSpread Spread, int VoteRounds, double? DiscussionMinutes);

/// <summary>
/// The public, read-only recap of a session. Only numbers, ticket titles and tracker keys: no descriptions,
/// links, notes or votes per player, and no names unless the host chose to show them.
/// </summary>
public sealed record RecapResponse(
    string GameName,
    DateTimeOffset PlayedOn,
    IReadOnlyList<RecapTicketItem> Tickets,
    int TicketsEstimated,
    int TicketsOpen,
    double TotalPoints,
    double? ConsensusRate,
    double? DurationMinutes,
    int PlayerCount,
    IReadOnlyList<string> Players);

public sealed record GetRecapSharingQuery(Guid GameId) : IRequest<Result<RecapSharingResponse, ApplicationError>>;
public sealed record SetRecapSharingCommand(Guid GameId, bool Shared, bool ShowNames) : IRequest<Result<RecapSharingResponse, ApplicationError>>, IAuditedRequest
{
    public AuditEntry? ToAuditEntry() => AuditEntry.Game(Shared ? AuditActions.RecapShared : AuditActions.RecapUnshared, AuditType.Update, GameId);
}
public sealed record GetRecapQuery(string Token) : IRequest<Result<RecapResponse, ApplicationError>>;

/// <summary>The link preview image of a shared recap (OPP-171): a 1200×630 PNG.</summary>
public sealed record GetRecapCardQuery(string Token) : IRequest<Result<byte[], ApplicationError>>;

public static class RecapErrors
{
    public static ApplicationError NotFound => new("RecapNotFound", "This recap is not shared or no longer exists.");
}

public static class Recap
{
    /// <summary>22 URL-safe characters from 128 random bits.</summary>
    public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>A token has the shape <see cref="NewToken"/> makes; anything else is not looked up.</summary>
    public static bool IsWellFormed(string? token) =>
        token is { Length: Game.RecapTokenLength } && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static RecapSharingResponse Sharing(Game game) => new(game.RecapToken is not null, game.RecapToken, game.RecapShowNames);

    public static double? Points(string? estimate) =>
        double.TryParse(estimate, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : null;

    /// <summary>Builds the recap from the tickets and revealed votes of a game. Pure, so it is unit tested.</summary>
    public static RecapResponse Build(Game game, IEnumerable<Ticket> tickets, IReadOnlyCollection<Vote> votes, IReadOnlyList<string> players)
    {
        var all = tickets.OrderBy(t => t.Position).ThenBy(t => t.CreatedOn).ToList();
        var votesByTicket = votes.GroupBy(v => v.TicketId).ToDictionary(g => g.Key, g => g.Select(v => v.Value).ToList());

        var items = new List<RecapTicketItem>();
        var consensus = 0;
        var compared = 0;
        foreach (var ticket in all.Where(t => t.IsEstimated))
        {
            var values = ticket.VotesRevealed && votesByTicket.TryGetValue(ticket.Id, out var v) ? v : [];
            var stats = TicketStats.From(ticket.VotesRevealed, values);
            if (values.Count >= 2)
            {
                compared++;
                if (stats.Spread == VoteSpread.Consensus) consensus++;
            }

            double? discussion = ticket.RevealedAt is { } r && ticket.EstimatedAt is { } e && e >= r
                ? Math.Round((e - r).TotalMinutes, 1)
                : null;
            items.Add(new RecapTicketItem(ticket.Name, ticket.ExternalKey, ticket.FinalEstimate ?? stats.Estimate, stats.Spread,
                ticket.VoteRounds, discussion));
        }

        var moments = all.SelectMany(t => new[] { t.RevealedAt, t.EstimatedAt }).Where(m => m is not null).Select(m => m!.Value).ToList();
        double? duration = moments.Count >= 2 ? Math.Round((moments.Max() - moments.Min()).TotalMinutes) : null;
        var playedOn = moments.Count > 0 ? moments.Min() : game.CreatedOn;

        var playerCount = votes.Where(v => all.Any(t => t.Id == v.TicketId && t.VotesRevealed)).Select(v => v.PlayerId).Distinct().Count();

        return new RecapResponse(
            game.Name,
            playedOn,
            items,
            items.Count,
            all.Count - items.Count,
            items.Sum(i => Points(i.Estimate) ?? 0),
            compared > 0 ? Math.Round((double)consensus / compared, 2) : null,
            duration is >= 1 ? duration : null,
            playerCount,
            players);
    }

    public sealed class GetSharingHandler(IGameRepository games) : IRequestHandler<GetRecapSharingQuery, Result<RecapSharingResponse, ApplicationError>>
    {
        public async Task<Result<RecapSharingResponse, ApplicationError>> Handle(GetRecapSharingQuery request, CancellationToken cancellationToken)
        {
            var game = await games.GetByIdAsync(request.GameId, cancellationToken);
            return game is null ? GameAccessErrors.GameNotFound : Sharing(game);
        }
    }

    public sealed class SetSharingHandler(IGameRepository games, IUnitOfWork unitOfWork) : IRequestHandler<SetRecapSharingCommand, Result<RecapSharingResponse, ApplicationError>>
    {
        public async Task<Result<RecapSharingResponse, ApplicationError>> Handle(SetRecapSharingCommand request, CancellationToken cancellationToken)
        {
            var game = await games.GetByIdAsync(request.GameId, cancellationToken);
            if (game is null)
            {
                return GameAccessErrors.GameNotFound;
            }

            if (request.Shared) game.ShareRecap(NewToken(), request.ShowNames);
            else game.StopSharingRecap();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Sharing(game);
        }
    }

    public sealed class GetHandler(IGameRepository games, ITicketRepository tickets, IVoteRepository votes, IUserService users)
        : IRequestHandler<GetRecapQuery, Result<RecapResponse, ApplicationError>>
    {
        public async Task<Result<RecapResponse, ApplicationError>> Handle(GetRecapQuery request, CancellationToken cancellationToken)
        {
            if (!IsWellFormed(request.Token))
            {
                return RecapErrors.NotFound;
            }

            var game = await games.GetByRecapTokenAsync(request.Token, cancellationToken);
            if (game is null)
            {
                return RecapErrors.NotFound;
            }

            var gameTickets = (await tickets.GetByGame(game.Id, cancellationToken)).ToList();
            var revealed = gameTickets.Where(t => t.VotesRevealed).Select(t => t.Id).ToHashSet();
            // Unrevealed votes never leave the room.
            var gameVotes = (await votes.GetByGame(game.Id, null, cancellationToken)).Where(v => revealed.Contains(v.TicketId)).ToList();

            var names = new List<string>();
            if (game.RecapShowNames)
            {
                foreach (var playerId in gameVotes.Select(v => v.PlayerId).Distinct())
                {
                    if ((await users.GetAsync($"{playerId}", cancellationToken))?.UserName is { Length: > 0 } name) names.Add(name);
                }
                names.Sort(StringComparer.CurrentCultureIgnoreCase);
            }

            return Build(game, gameTickets, gameVotes, names);
        }
    }

    public sealed class CardHandler(ISender sender) : IRequestHandler<GetRecapCardQuery, Result<byte[], ApplicationError>>
    {
        public async Task<Result<byte[], ApplicationError>> Handle(GetRecapCardQuery request, CancellationToken cancellationToken)
        {
            var recap = await sender.Send(new GetRecapQuery(request.Token), cancellationToken);
            return recap.IsSuccess ? RecapCard.Render(recap.Value!) : recap.Error!;
        }
    }
}
