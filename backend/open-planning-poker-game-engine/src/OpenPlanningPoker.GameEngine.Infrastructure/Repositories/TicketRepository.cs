namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class TicketRepository(OpenPlanningPokerGameEngineDbContext dbContext)
    : Repository<Ticket, Guid>(dbContext), ITicketRepository
{
    public async Task<IEnumerable<Ticket>> GetByGame(Guid gameId, CancellationToken cancellationToken = default)
        => await DbContext.Set<Ticket>()
            .QueryByGame(gameId)
            .ToListAsync(cancellationToken);
}