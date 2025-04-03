namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Info;

[QueryType]
public class InfoQueries
{
    /// <summary>
    /// Returns an info about the Game Engine service.
    /// </summary>
    public async Task<FieldResult<Info, ApplicationError>> GetInfoAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInfoQuery(), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Info>(result.Value)
            : result.Error!;
    }
}
