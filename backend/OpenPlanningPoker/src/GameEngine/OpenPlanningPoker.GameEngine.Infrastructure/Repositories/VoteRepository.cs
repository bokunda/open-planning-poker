namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public class VoteRepository(OpenPlanningPokerGameEngineDbContext dbContext)
    : Repository<Vote, Guid>(dbContext), IVoteRepository
{
    public async Task<ICollection<Vote>> GetByTicket(Guid ticketId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<Vote>()
            .QueryByTicket(ticketId)
            .ToListAsync(cancellationToken);
}