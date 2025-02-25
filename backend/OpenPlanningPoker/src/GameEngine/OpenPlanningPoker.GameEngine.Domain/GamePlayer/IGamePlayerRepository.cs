namespace OpenPlanningPoker.GameEngine.Domain.GamePlayer;

public interface IGamePlayerRepository : IRepository<GamePlayer, Guid>
{
    Task<GamePlayer> GetByGameAndPlayer(Guid gameId, Guid playerId, CancellationToken cancellationToken = default);
    Task<ICollection<GamePlayer>> GetByGame(Guid gameId, CancellationToken cancellationToken = default);
}