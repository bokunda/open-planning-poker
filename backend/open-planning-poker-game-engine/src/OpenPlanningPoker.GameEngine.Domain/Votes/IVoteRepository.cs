﻿namespace OpenPlanningPoker.GameEngine.Domain.Votes;

public interface IVoteRepository : IRepository<Vote, Guid>
{
    Task<ICollection<Vote>> GetByTicket(Guid ticketId, Guid? playerId, CancellationToken cancellationToken = default);
    Task<ICollection<Vote>> GetByGame(Guid gameId, Guid? playerId, CancellationToken cancellationToken = default);
}