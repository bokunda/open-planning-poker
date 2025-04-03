namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

[MutationType]
public class VoteMutations
{
    /// <summary>
    /// Creates or updates a vote.
    /// </summary>
    public async Task<FieldResult<Vote, ApplicationError>> CreateOrUpdateVoteAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Service] ITopicEventSender eventSender,
        [Required] Guid ticketId,
        [Required] string value,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CreateOrUpdateVoteCommand(ticketId, value), cancellationToken);

        if (result.IsSuccess)
        {
            await eventSender.SendAsync(
                $"{nameof(CreateOrUpdateVoteAsync)}_{ticketId}",
                mapper.Map<Vote>(result.Value),
                cancellationToken);
        }

        return result.IsSuccess
            ? mapper.Map<Vote>(result.Value)
            : result.Error!;
    }
}
