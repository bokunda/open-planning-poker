namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class TicketRepository(OpenPlanningPokerGameEngineDbContext dbContext)
    : Repository<Ticket, Guid>(dbContext), ITicketRepository
{
    public async Task<IEnumerable<Ticket>> GetByGame(Guid gameId, CancellationToken cancellationToken = default)
        => await DbContext.Set<Ticket>()
            .QueryByGame(gameId)
            .OrderBy(ticket => ticket.Position)
            .ThenBy(ticket => ticket.CreatedOn)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Ticket>> GetByGames(IReadOnlyCollection<Guid> gameIds, CancellationToken cancellationToken = default)
        => await DbContext.Set<Ticket>()
            .Where(ticket => gameIds.Contains(ticket.GameId))
            .ToListAsync(cancellationToken);
}