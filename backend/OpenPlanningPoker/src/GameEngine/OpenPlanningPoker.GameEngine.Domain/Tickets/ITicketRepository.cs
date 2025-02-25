namespace OpenPlanningPoker.GameEngine.Domain.Tickets;

public interface ITicketRepository : IRepository<Ticket, Guid>
{
    Task<IEnumerable<Ticket>> GetByGame(Guid gameId, CancellationToken cancellationToken = default);
}