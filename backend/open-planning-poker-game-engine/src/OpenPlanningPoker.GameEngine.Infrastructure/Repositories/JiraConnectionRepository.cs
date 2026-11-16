using OpenPlanningPoker.GameEngine.Domain.Integrations;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class JiraConnectionRepository(OpenPlanningPokerGameEngineDbContext dbContext) : IJiraConnectionRepository
{
    public Task<JiraConnection?> GetByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        dbContext.Set<JiraConnection>().FirstOrDefaultAsync(connection => connection.PlayerId == playerId, cancellationToken);

    public void Add(JiraConnection connection) => dbContext.Set<JiraConnection>().Add(connection);

    public void Remove(JiraConnection connection) => dbContext.Set<JiraConnection>().Remove(connection);
}
