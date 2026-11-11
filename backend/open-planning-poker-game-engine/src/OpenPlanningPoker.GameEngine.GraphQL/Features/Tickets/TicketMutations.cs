namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets;

[MutationType]
public class TicketMutations
{
    /// <summary>
    /// Creates a ticket for given game. Host only.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> CreateTicketAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] string name,
        [Required] string description,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new CreateTicketCommand(gameId, name, description), cancellationToken);

        if (result.IsSuccess)
        {
            await eventSender.SendAsync(
                $"{nameof(CreateTicketAsync)}_{gameId}",
                mapper.Map<Ticket>(result.Value),
                cancellationToken);
        }

        return result.IsSuccess
            ? mapper.Map<Ticket>(result.Value)
            : result.Error!;
    }

    /// <summary>
    /// Updates a ticket for given game. Host only.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> UpdateTicketAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Service] IGameAccessService gameAccess,
        [Required] Guid id,
        [Required] string name,
        [Required] string description,
        CancellationToken cancellationToken = default)
    {
        if ((await gameAccess.EnsureHostOfTicketAsync(id, cancellationToken)).Error is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new UpdateTicketCommand(id, name, description), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Ticket>(result.Value)
            : result.Error!;
    }

    /// <summary>
    /// Re-votes a ticket: deletes its votes, hides voting again and broadcasts it as the
    /// active ticket to all players in the game. Host only.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> ReVoteTicketAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var (ticketGameId, accessError) = await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        if (ticketGameId != gameId)
        {
            return GameAccessErrors.TicketNotFound;
        }

        var resetResult = await sender.Send(new ResetTicketVotesCommand(ticketId), cancellationToken);
        if (!resetResult.IsSuccess)
        {
            return resetResult.Error!;
        }

        var result = await sender.Send(new GetTicketQuery(ticketId), cancellationToken);

        if (result.IsSuccess)
        {
            await eventSender.SendAsync(
                $"{nameof(ReVoteTicketAsync)}_{ticketGameId}",
                mapper.Map<Ticket>(result.Value),
                cancellationToken);
        }

        return result.IsSuccess
            ? mapper.Map<Ticket>(result.Value)
            : result.Error!;
    }

    /// <summary>
    /// Deletes a ticket for given game. Host only.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> DeleteTicketAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Service] IGameAccessService gameAccess,
        [Required] Guid id,
        CancellationToken cancellationToken = default)
    {
        if ((await gameAccess.EnsureHostOfTicketAsync(id, cancellationToken)).Error is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new DeleteTicketCommand(id), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Ticket>(result.Value)
            : result.Error!;
    }
}
