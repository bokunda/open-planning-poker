namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

[QueryType]
public class VoteQueries
{
    /// <summary>
    /// Gets votes by ticketId.
    /// </summary>
    public async Task<FieldResult<ApiCollection<Vote>, ApplicationError>> GetVotesAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetVotesQuery(ticketId), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<ApiCollection<Vote>>(result.Value)
            : result.Error!;
    }
}
