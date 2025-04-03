namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class GameSettingsRepository(OpenPlanningPokerGameEngineDbContext dbContext)
    : Repository<GameSettings, Guid>(dbContext), IGameSettingsRepository
{
    public async Task<GameSettings> GetByGame(Guid gameId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<GameSettings>()
            .QueryByGame(gameId)
            .FirstAsync(cancellationToken);

}