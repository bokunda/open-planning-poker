namespace OpenPlanningPoker.GameEngine.Domain.GamePlayer;

public sealed class GamePlayer : Entity<Guid>
{
    private GamePlayer(Guid gameId, Guid playerId)
    {
        GameId = gameId;
        PlayerId = playerId;
    }

    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }

    public Game Game { get; set; } = null!;

    public static GamePlayer Create(Guid gameId, Guid playerId) => new (gameId, playerId);
}