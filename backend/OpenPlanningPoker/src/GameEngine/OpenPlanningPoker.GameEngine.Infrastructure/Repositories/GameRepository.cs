namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class GameRepository(OpenPlanningPokerGameEngineDbContext dbContext)
    : Repository<Game, Guid>(dbContext), IGameRepository;