namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

[MutationType]
public class VoteMutations
{
    /// <summary>
    /// Creates a vote.
    /// </summary>
    public async Task<FieldResult<Vote, ApplicationError>> CreateVoteAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid ticketId,
        [Required] string value,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CreateVoteCommand(ticketId, value), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Vote>(result.Value)
            : result.Error!;
    }

    /// <summary>
    /// Updates a vote.
    /// </summary>
    public async Task<FieldResult<Vote, ApplicationError>> UpdateVoteAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid id,
        [Required] string value,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new UpdateVoteCommand(id, value), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Vote>(result.Value)
            : result.Error!;
    }
}
