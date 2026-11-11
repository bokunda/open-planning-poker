namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Info;

[QueryType]
public class InfoQueries
{
    /// <summary>
    /// Returns an info about the Game Engine service.
    /// </summary>
    public async Task<FieldResult<Info, ApplicationError>> GetInfoAsync(
        [Service] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInfoQuery(), cancellationToken);
        return result.IsSuccess
            ? result.Value!.ToGraphQL()
            : result.Error!;
    }
}
