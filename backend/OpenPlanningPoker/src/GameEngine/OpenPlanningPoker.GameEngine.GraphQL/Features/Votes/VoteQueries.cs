namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes
{
    [QueryType]
    public class VoteQueries
    {
        /// <summary>
        /// Gets votes by ticketId.
        /// </summary>
        public async Task<ApiCollection<Vote>> GetVotesAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid ticketId,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new GetVotesQuery(ticketId), cancellationToken);
            return mapper.Map<ApiCollection<Vote>>(result);
        }
    }
}
