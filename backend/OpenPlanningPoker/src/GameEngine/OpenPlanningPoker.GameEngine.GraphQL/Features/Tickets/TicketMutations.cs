namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets;

[MutationType]
public class TicketMutations
{
    /// <summary>
    /// Creates a ticket for given game.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> CreateTicketAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid gameId,
        [Required] string name,
        [Required] string description,
        CancellationToken cancellationToken = default)
    {
        //TODO: Access check -> player <-> game
        var result = await sender.Send(new CreateTicketCommand(gameId, name, description), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Ticket>(result.Value)
            : result.Error!;
    }

    /// <summary>
    /// Updates a ticket for given game.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> UpdateTicketAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid id,
        [Required] string name,
        [Required] string description,
        CancellationToken cancellationToken = default)
    {
        //TODO: Access check -> player <-> game
        var result = await sender.Send(new UpdateTicketCommand(id, name, description), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Ticket>(result.Value)
            : result.Error!;
    }

    /// <summary>
    /// Deletes a ticket for given game.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> DeleteTicketAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid id,
        CancellationToken cancellationToken = default)
    {
        //TODO: Access check -> player <-> game
        var result = await sender.Send(new DeleteTicketCommand(id), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Ticket>(result.Value)
            : result.Error!;
    }
}
