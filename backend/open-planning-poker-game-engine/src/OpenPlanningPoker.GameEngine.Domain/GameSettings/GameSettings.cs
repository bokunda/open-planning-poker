namespace OpenPlanningPoker.GameEngine.Domain.GameSettings;

public sealed class GameSettings : Entity<Guid>
{
    internal GameSettings(Guid gameId, string deckSetup)
    {
        GameId = gameId;
        DeckSetup = deckSetup;
    }

    public Guid GameId { get; private set; }
    public string DeckSetup { get; private set; }

    /// <summary>Reveal the cards as soon as every player (not observers) has voted.</summary>
    public bool AutoReveal { get; private set; }

    /// <summary>
    /// While set and in the future, players vote on the tickets on their own before the meeting.
    /// Votes stay hidden until the host reveals each ticket.
    /// </summary>
    public DateTimeOffset? AsyncVotingEndsAt { get; private set; }

    public Game Game { get; set; } = null!;

    public static GameSettings Create(Guid gameId, string deckSetup)
    {
        var gameSettings = new GameSettings(gameId, deckSetup);
        gameSettings.RaiseDomainEvent(new CreateGameSettingsDomainEvent(gameSettings.Id));
        return gameSettings;
    }

    public void Update(Guid gameId, string deckSetup)
    {
        GameId = gameId;
        DeckSetup = deckSetup;
        RaiseDomainEvent(new CreateGameSettingsDomainEvent(Id));
    }

    public void UpdateOptions(bool autoReveal, DateTimeOffset? asyncVotingEndsAt)
    {
        AutoReveal = autoReveal;
        AsyncVotingEndsAt = asyncVotingEndsAt;
    }
}
