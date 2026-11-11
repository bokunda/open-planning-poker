namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

[MutationType]
public class VoteMutations
{
    /// <summary>
    /// Creates or updates a vote. Only players who joined the game can vote.
    /// </summary>
    public async Task<FieldResult<Vote, ApplicationError>> CreateOrUpdateVoteAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        [Required] string value,
        CancellationToken cancellationToken = default)
    {
        if ((await gameAccess.EnsureMemberOfTicketAsync(ticketId, cancellationToken)).Error is { } accessError)
        {
            return accessError;
        }

        if (rateLimiter.TryAcquire(ActionRateLimiter.Vote, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        var result = await sender.Send(new CreateOrUpdateVoteCommand(ticketId, value), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        var vote = result.Value!.ToGraphQL();

        // The broadcast goes to every subscriber, so it carries the value only once votes are revealed.
        var ticket = await sender.Send(new GetTicketQuery(ticketId), cancellationToken);
        var broadcastVote = ticket.Value?.VotesRevealed == true
            ? vote
            : new Vote { Id = vote.Id, PlayerId = vote.PlayerId, Value = GetVotes.HiddenVoteValue };

        await eventSender.SendAsync(
            $"{nameof(CreateOrUpdateVoteAsync)}_{ticketId}",
            broadcastVote,
            cancellationToken);

        return vote;
    }

    /// <summary>
    /// Reveals all votes for a ticket and broadcasts them to all players. Host only.
    /// </summary>
    public async Task<FieldResult<VotesRevealed, ApplicationError>> RevealVotesAsync(
        [Service] ISender sender,
        [Service] ICurrentUserProvider currentUserProvider,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        if ((await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken)).Error is { } accessError)
        {
            return accessError;
        }

        var revealResult = await sender.Send(new SetTicketVotesRevealedCommand(ticketId, true), cancellationToken);
        if (!revealResult.IsSuccess)
        {
            return revealResult.Error!;
        }

        var votes = await sender.Send(new GetVotesQuery(ticketId), cancellationToken);
        var user = await currentUserProvider.GetAsync(cancellationToken);

        var reveal = new VotesRevealed
        {
            TicketId = ticketId,
            RevealedBy = user.Value?.UserName ?? "Host",
            Votes = votes.IsSuccess ? [.. votes.Value!.Items.Select(item => item.ToGraphQL())] : []
        };

        await eventSender.SendAsync(
            $"{nameof(RevealVotesAsync)}_{ticketId}",
            reveal,
            cancellationToken);

        return reveal;
    }
}
