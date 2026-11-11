namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

[QueryType]
public class GameQueries
{
    /// <summary>
    /// Returns Game details.
    /// </summary>
    public async Task<FieldResult<Game, ApplicationError>> GetGameAsync(
        [Service] ISender sender,
        [Required] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetGameQuery(id), cancellationToken);
        return result.IsSuccess
            ? result.Value!.ToGraphQL()
            : result.Error!;
    }
}
