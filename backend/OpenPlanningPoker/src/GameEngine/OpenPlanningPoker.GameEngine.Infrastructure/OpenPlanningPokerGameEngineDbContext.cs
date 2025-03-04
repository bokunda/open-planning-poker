namespace OpenPlanningPoker.GameEngine.Infrastructure;

public sealed class OpenPlanningPokerGameEngineDbContext(
    DbContextOptions options,
    ICurrentUserProvider currentUserProvider,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OpenPlanningPokerGameEngineDbContext).Assembly);

        // Seed the data
        DbInitializer.SeedData(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        try
        {
            SetCreateOnUpdateOn();

            var result = await base.SaveChangesAsync(cancellationToken);

            // Re-think, should publish domain events or save changes first? 
            // Because we can get exceptions in both ways.
            // Outbox pattern is a better approach
            await PublishDomainEventAsync();

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Concurrency exception occurred!", ex);
        }
    }

    private void SetCreateOnUpdateOn()
    {
        var utcNow = dateTimeProvider.UtcNow;
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is IEntityHasCreated && e.State is EntityState.Added or EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                ((IEntityHasCreated)entityEntry.Entity).SetCreated(utcNow, currentUserProvider.UserId);
            }
        }
    }

    private async Task PublishDomainEventAsync()
    {
        var domainEvents = ChangeTracker
            .Entries<IEntity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                var domainEvents = entity.GetDomainEvents();

                // Important because we don't know what will happen inside specific domain event handlers
                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent);
        }
    }
}