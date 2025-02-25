namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets
{
    [MutationType]
    public class TicketMutations
    {
        /// <summary>
        /// Creates a ticket for given game.
        /// </summary>
        public async Task<Ticket> CreateTicketAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid gameId,
            [Required] string name,
            [Required] string description,
            CancellationToken cancellationToken = default)
        {
            //TODO: Access check -> player <-> game
            var result = await sender.Send(new CreateTicketCommand(gameId, name, description), cancellationToken);
            return mapper.Map<Ticket>(result);
        }

        /// <summary>
        /// Updates a ticket for given game.
        /// </summary>
        public async Task<Ticket> UpdateTicketAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid id,
            [Required] string name,
            [Required] string description,
            CancellationToken cancellationToken = default)
        {
            //TODO: Access check -> player <-> game
            var result = await sender.Send(new UpdateTicketCommand(id, name, description), cancellationToken);
            return mapper.Map<Ticket>(result);
        }

        /// <summary>
        /// Deletes a ticket for given game.
        /// </summary>
        public async Task<Ticket> DeleteTicketAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid id,
            CancellationToken cancellationToken = default)
        {
            //TODO: Access check -> player <-> game
            var result = await sender.Send(new DeleteTicketCommand(id), cancellationToken);
            return mapper.Map<Ticket>(result);
        }
    }
}
