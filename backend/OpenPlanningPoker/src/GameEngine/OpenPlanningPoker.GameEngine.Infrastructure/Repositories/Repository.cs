namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public class Repository<TEntity, TEntityId>(OpenPlanningPokerGameEngineDbContext dbContext)
    : IRepository<TEntity, TEntityId>
    where TEntity : Entity<TEntityId>
{
    protected readonly OpenPlanningPokerGameEngineDbContext DbContext = dbContext;

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbContext
            .Set<TEntity>()
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<TEntityId> ids, CancellationToken cancellationToken = default) =>
        await DbContext
            .Set<TEntity>()
            .Where(entity => ids.Contains(entity.Id))
            .ToListAsync(cancellationToken);

    public async Task<TEntity?> GetByIdAsync(TEntityId id, CancellationToken cancellationToken = default) =>
        await DbContext
            .Set<TEntity>()
            .FirstOrDefaultAsync(entity => entity.Id!.Equals(id), cancellationToken);

    public void Add(TEntity entity) => DbContext.Set<TEntity>().Add(entity);

    public void AddRange(IEnumerable<TEntity> entities) => DbContext.Set<TEntity>().AddRange(entities);

    public void Update(TEntity entity)
    {
        DbContext.Set<TEntity>().Attach(entity);
        DbContext.Entry(entity).State = EntityState.Modified;
    }

    public void Delete(TEntity entity) => DbContext.Set<TEntity>().Remove(entity);
    public void DeleteRange(IEnumerable<TEntity> entities) => DbContext.Set<TEntity>().RemoveRange(entities);
}