namespace OpenPlanningPoker.GameEngine.Domain.Tickets;

public static class TicketQueryableExtensions
{
    public static IQueryable<Ticket> QueryByGame(this IQueryable<Ticket> query, Guid gameId)
        => query.Where(ticket => ticket.GameId == gameId);
}