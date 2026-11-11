namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

[QueryType]
public class VoteQueries
{
    /// <summary>
    /// Gets votes by ticketId.
    /// </summary>
    public async Task<FieldResult<ApiCollection<Vote>, ApplicationError>> GetVotesAsync(
        [Service] ISender sender,
        [Required] Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetVotesQuery(ticketId), cancellationToken);
        return result.IsSuccess
            ? result.Value.ToGraphQL(item => item.ToGraphQL())
            : result.Error!;
    }
}
