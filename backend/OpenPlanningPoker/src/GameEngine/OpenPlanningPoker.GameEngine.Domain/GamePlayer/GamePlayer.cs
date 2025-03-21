using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.GamePlayer;

public sealed class GamePlayer : Entity<Guid>
{
    private GamePlayer(Guid Id, Guid gameId, Guid playerId)
    {
        GameId = gameId;
        PlayerId = playerId;
    }

    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }

    public Game Game { get; set; } = null!;

    public static GamePlayer Create(Guid gameId, Guid playerId) => new (SequentialGuidGenerator.Generate(), gameId, playerId);
}