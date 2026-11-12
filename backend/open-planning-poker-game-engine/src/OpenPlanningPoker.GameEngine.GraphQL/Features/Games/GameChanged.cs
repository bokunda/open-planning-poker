namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

/// <summary>What changed in a game. Clients reload that part (players, tickets or the game and its settings).</summary>
public enum GameChangeKind
{
    /// <summary>Host, settings or options changed: reload the game.</summary>
    Game,

    /// <summary>A player joined, left, was removed, or changed role / co-host.</summary>
    Players,

    /// <summary>Tickets were edited, deleted, reordered or got a final estimate.</summary>
    Tickets,

    /// <summary>A player was removed; <see cref="GameChanged.PlayerId"/> says who.</summary>
    PlayerRemoved,
}

/// <summary>
/// A small "something changed" event. It carries no data that is not already public in the game,
/// so it is safe to broadcast; clients fetch the details with their own (checked) queries.
/// </summary>
public sealed class GameChanged
{
    public Guid GameId { get; set; }
    public GameChangeKind Kind { get; set; }
    public Guid? TicketId { get; set; }
    public Guid? PlayerId { get; set; }
}

public static class GameEvents
{
    public static string Topic(Guid gameId) => $"GameChanged_{gameId}";

    public static ValueTask PublishAsync(
        this ITopicEventSender eventSender,
        Guid gameId,
        GameChangeKind kind,
        Guid? ticketId = null,
        Guid? playerId = null,
        CancellationToken cancellationToken = default) =>
        eventSender.SendAsync(
            Topic(gameId),
            new GameChanged { GameId = gameId, Kind = kind, TicketId = ticketId, PlayerId = playerId },
            cancellationToken);
}

[SubscriptionType]
public class GameSubscriptions
{
    /// <summary>Fires when players, tickets or the game itself change; reload the part named by <c>kind</c>.</summary>
    [Subscribe]
    [Topic("GameChanged_{gameId}")]
    public GameChanged OnGameChanged(Guid gameId, [EventMessage] GameChanged change) => change;
}
