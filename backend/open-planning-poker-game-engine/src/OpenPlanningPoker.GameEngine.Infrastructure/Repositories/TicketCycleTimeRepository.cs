namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class TicketCycleTimeRepository(OpenPlanningPokerGameEngineDbContext dbContext) : ITicketCycleTimeRepository
{
    public async Task<IReadOnlyList<TicketCycleTime>> GetAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TicketCycleTime>().Where(c => ticketIds.Contains(c.TicketId)).ToListAsync(cancellationToken);

    public void Add(TicketCycleTime cycleTime) => dbContext.Set<TicketCycleTime>().Add(cycleTime);
}
