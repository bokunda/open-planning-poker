namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class AuditRepository(OpenPlanningPokerGameEngineDbContext dbContext) : IAuditRepository
{
    public void Add(Audit audit) => dbContext.Set<Audit>().Add(audit);

    public void Discard(Audit audit) => dbContext.Entry(audit).State = EntityState.Detached;

    public async Task<IReadOnlyList<Audit>> GetAsync(AuditScope scope, Guid scopeId, DateTimeOffset? before, int take, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Audit>().AsNoTracking().Where(audit => audit.Scope == scope && audit.ScopeId == scopeId);
        if (before is { } cursor)
        {
            query = query.Where(audit => audit.CreatedOn < cursor);
        }

        return await query
            .OrderByDescending(audit => audit.CreatedOn)
            .ThenByDescending(audit => audit.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        dbContext.Set<Audit>().Where(audit => audit.CreatedOn < cutoff).ExecuteDeleteAsync(cancellationToken);
}
