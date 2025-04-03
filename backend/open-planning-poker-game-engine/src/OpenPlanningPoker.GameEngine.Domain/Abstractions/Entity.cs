namespace OpenPlanningPoker.GameEngine.Domain.Abstractions;

public abstract class Entity<TEntityId> : IEntity, IEntityHasCreated
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected Entity(TEntityId id)
    {
        Id = id;
    }

#pragma warning disable CS8618
    protected Entity() { }
#pragma warning restore CS8618

    public TEntityId Id { get; init; }
    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedOn { get; private set; }
    public bool IsDeleted { get; private set; }

    public void SetIsDeleted(bool isDeleted = true) => IsDeleted = isDeleted;
    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.ToList();
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void SetCreated(DateTimeOffset createdOn, Guid createdBy)
    {
        CreatedOn = createdOn;
        CreatedBy = createdBy;
    }
}