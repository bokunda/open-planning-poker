namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class GameRepository(OpenPlanningPokerGameEngineDbContext dbContext)
    : Repository<Game, Guid>(dbContext), IGameRepository
{
    public async Task<IReadOnlyList<Game>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<Game>()
            .Where(game => game.TeamId == teamId)
            .OrderByDescending(game => game.CreatedOn)
            .ToListAsync(cancellationToken);
}