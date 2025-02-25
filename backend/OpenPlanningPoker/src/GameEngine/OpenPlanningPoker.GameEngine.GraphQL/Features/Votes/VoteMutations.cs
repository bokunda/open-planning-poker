namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes
{
    [MutationType]
    public class VoteMutations
    {
        /// <summary>
        /// Creates a vote.
        /// </summary>
        public async Task<Vote> CreateVoteAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid ticketId,
            [Required] int value,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new CreateVoteCommand(ticketId, value), cancellationToken);
            return mapper.Map<Vote>(result);
        }

        /// <summary>
        /// Updates a vote.
        /// </summary>
        public async Task<Vote> UpdateVoteAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid id,
            [Required] int value,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new UpdateVoteCommand(id, value), cancellationToken);
            return mapper.Map<Vote>(result);
        }
    }
}
