namespace OpenPlanningPoker.GameEngine.Domain.Repositories;

public interface IRepository<TEntity, in TEntityId>
    where TEntity : Entity<TEntityId>
{
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<TEntityId> ids, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(TEntityId id, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    void DeleteRange(IEnumerable<TEntity> entities);
}