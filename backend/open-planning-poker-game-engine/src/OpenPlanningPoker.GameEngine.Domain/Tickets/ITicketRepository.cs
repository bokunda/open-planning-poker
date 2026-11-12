namespace OpenPlanningPoker.GameEngine.Domain.Tickets;

public interface ITicketRepository : IRepository<Ticket, Guid>
{
    /// <summary>Tickets of a game in list order.</summary>
    Task<IEnumerable<Ticket>> GetByGame(Guid gameId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Ticket>> GetByGames(IReadOnlyCollection<Guid> gameIds, CancellationToken cancellationToken = default);
}