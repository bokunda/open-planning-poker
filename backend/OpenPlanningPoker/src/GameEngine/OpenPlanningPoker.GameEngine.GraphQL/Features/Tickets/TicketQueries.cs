namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets
{
    [QueryType]
    public class TicketQueries
    {
        /// <summary>
        /// Gets a ticket by id.
        /// </summary>
        public async Task<Ticket> GetTicketAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new GetTicketQuery(id), cancellationToken);
            return mapper.Map<Ticket>(result);
        }

        /// <summary>
        /// Gets a list of tickets by gameId.
        /// </summary>
        public async Task<ApiCollection<Ticket>> GetTicketsAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid gameId,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new GetTicketsQuery(gameId), cancellationToken);
            return mapper.Map<ApiCollection<Ticket>>(result);
        }
    }
}
