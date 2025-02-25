using OpenPlanningPoker.GameEngine.Domain.GameSettings.Events;

namespace OpenPlanningPoker.GameEngine.Domain.GameSettings;

public sealed class GameSettings : Entity<Guid>
{
    internal GameSettings(Guid gameId, int votingTime, bool isBreakAllowed)
    {
        GameId = gameId;
        VotingTime = votingTime;
        IsBreakAllowed = isBreakAllowed;
    }

    public Guid GameId { get; private set; }
    public int VotingTime { get; private set; }
    public bool IsBreakAllowed { get; private set; }

    public Game Game { get; set; } = null!;

    public static GameSettings Create(Guid gameId, int votingTime, bool isBreakAllowed)
    {
        var gameSettings = new GameSettings(gameId, votingTime, isBreakAllowed);
        gameSettings.RaiseDomainEvent(new CreateGameSettingsDomainEvent(gameSettings.Id));
        return gameSettings;
    }

    public void Update(Guid gameId, int votingTime, bool isBreakAllowed)
    {
        GameId = gameId;
        VotingTime = votingTime;
        IsBreakAllowed = isBreakAllowed;
        RaiseDomainEvent(new CreateGameSettingsDomainEvent(Id));
    }
}