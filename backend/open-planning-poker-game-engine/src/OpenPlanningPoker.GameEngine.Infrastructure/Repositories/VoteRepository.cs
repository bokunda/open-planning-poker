﻿namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public class VoteRepository(OpenPlanningPokerGameEngineDbContext dbContext)
    : Repository<Vote, Guid>(dbContext), IVoteRepository
{
    public async Task<ICollection<Vote>> GetByTicket(Guid ticketId, Guid? playerId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<Vote>()
            .QueryByTicket(ticketId)
            .QueryByPlayer(playerId)
            .ToListAsync(cancellationToken);

    public async Task<ICollection<Vote>> GetByGame(Guid gameId, Guid? playerId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<Vote>()
            .QueryByGame(gameId)
            .QueryByPlayer(playerId)
            .ToListAsync(cancellationToken);
}