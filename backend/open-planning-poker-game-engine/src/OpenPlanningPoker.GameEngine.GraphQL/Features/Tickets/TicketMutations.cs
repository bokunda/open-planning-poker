using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets;

[MutationType]
public class TicketMutations
{
    /// <summary>
    /// Creates a ticket for given game. Host only.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> CreateTicketAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
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

        if (rateLimiter.TryAcquire(ActionRateLimiter.CreateTicket, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        var result = await sender.Send(new CreateTicketCommand(gameId, name, description), cancellationToken);

        if (result.IsSuccess)
        {
            await eventSender.SendAsync(
                $"{nameof(CreateTicketAsync)}_{gameId}",
                result.Value!.ToGraphQL(),
                cancellationToken);
        }

        return result.IsSuccess
            ? result.Value!.ToGraphQL()
            : result.Error!;
    }

    /// <summary>
    /// Updates a ticket's name and description. Host and co-hosts.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> UpdateTicketAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid id,
        [Required] string name,
        [Required] string description,
        CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureHostOfTicketAsync(id, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        var result = await sender.Send(new UpdateTicketCommand(id, name, description), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, ticketId: id, cancellationToken: cancellationToken);
        return result.Value!.ToGraphQL();
    }

    /// <summary>
    /// Puts the game's tickets in the given order (first id first). Host and co-hosts.
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> ReorderTicketsAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] List<Guid> ticketIds,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new ReorderTicketsCommand(gameId, ticketIds), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>
    /// Sets the estimate the team agreed on after the reveal (any value, usually a card). An empty value clears it.
    /// Host and co-hosts.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> SetFinalEstimateAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        string? value,
        CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        var result = await sender.Send(new SetFinalEstimateCommand(ticketId, value), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, ticketId: ticketId, cancellationToken: cancellationToken);
        return result.Value!.ToGraphQL();
    }

    /// <summary>
    /// Re-votes a ticket: deletes its votes, hides voting again and broadcasts it as the
    /// active ticket to all players in the game. Host only.
    /// </summary>
    public async Task<FieldResult<Ticket, ApplicationError>> ReVoteTicketAsync(
        [Service] ISender sender,
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
                result.Value!.ToGraphQL(),
                cancellationToken);
        }

        return result.IsSuccess
            ? result.Value!.ToGraphQL()
            : result.Error!;
    }

    /// <summary>
    /// Deletes a ticket for given game. Host only.
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> DeleteTicketAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid id,
        CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureHostOfTicketAsync(id, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        var result = await sender.Send(new DeleteTicketCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, ticketId: id, cancellationToken: cancellationToken);
        return result.Value;
    }
}
