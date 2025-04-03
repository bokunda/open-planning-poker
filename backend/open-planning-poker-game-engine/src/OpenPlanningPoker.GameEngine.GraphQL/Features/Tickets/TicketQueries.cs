namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets;

[QueryType]
public class TicketQueries
{
    /// <summary>
    /// Gets a ticket by id.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> GetTicketAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTicketQuery(id), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Ticket>(result.Value)
            : result.Error!;
    }

    /// <summary>
    /// Gets a list of tickets by gameId.
    /// </summary>
    public async Task<FieldResult<ApiCollection<Ticket>, ApplicationError>> GetTicketsAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTicketsQuery(gameId), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<ApiCollection<Ticket>>(result.Value)
            : result.Error!;
    }
}
