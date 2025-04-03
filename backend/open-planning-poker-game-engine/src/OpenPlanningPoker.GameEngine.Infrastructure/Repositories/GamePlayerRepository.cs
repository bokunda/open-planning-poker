namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class GamePlayerRepository(OpenPlanningPokerGameEngineDbContext dbContext)
    : Repository<GamePlayer, Guid>(dbContext), IGamePlayerRepository
{
    public async Task<GamePlayer> GetByGameAndPlayer(Guid gameId, Guid playerId, CancellationToken cancellationToken = default)
        => await DbContext.Set<GamePlayer>()
            .QueryByGame(gameId)
            .QueryByPlayer(playerId)
            .SingleAsync(cancellationToken);

    public async Task<ICollection<GamePlayer>> GetByGame(Guid gameId, CancellationToken cancellationToken = default)
        => await DbContext.Set<GamePlayer>()
            .QueryByGame(gameId)
            .ToListAsync(cancellationToken);
}