namespace OpenPlanningPoker.GameEngine.Domain.Votes;

public interface IVoteRepository : IRepository<Vote, Guid>
{
    Task<ICollection<Vote>> GetByTicket(Guid ticketId, CancellationToken cancellationToken = default);
}