using OpenPlanningPoker.GameEngine.Domain.Integrations;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class TrackerConnectionRepository(OpenPlanningPokerGameEngineDbContext dbContext) : ITrackerConnectionRepository
{
    public Task<TrackerConnection?> GetAsync(Guid playerId, string provider, CancellationToken cancellationToken = default) =>
        dbContext.Set<TrackerConnection>().FirstOrDefaultAsync(c => c.PlayerId == playerId && c.Provider == provider, cancellationToken);

    public void Add(TrackerConnection connection) => dbContext.Set<TrackerConnection>().Add(connection);

    public void Remove(TrackerConnection connection) => dbContext.Set<TrackerConnection>().Remove(connection);
}
