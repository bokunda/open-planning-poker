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
}