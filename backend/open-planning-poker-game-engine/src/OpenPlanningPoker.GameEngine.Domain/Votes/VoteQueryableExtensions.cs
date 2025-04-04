﻿namespace OpenPlanningPoker.GameEngine.Domain.Votes;

public static class VoteQueryableExtensions
{
    public static IQueryable<Vote> QueryByTicket(this IQueryable<Vote> query, Guid ticketId)
        => query.Where(x => x.TicketId == ticketId);

    public static IQueryable<Vote> QueryByPlayer(this IQueryable<Vote> query, Guid? playerId)
        => playerId.HasValue
            ? query.Where(x => x.PlayerId == playerId)
            : query;
}