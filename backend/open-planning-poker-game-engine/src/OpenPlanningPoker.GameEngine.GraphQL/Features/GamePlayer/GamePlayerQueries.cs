namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer;

[QueryType]
public class GamePlayerQueries
{
    /// <summary>
    /// Returns game with participants
    /// </summary>
    public async Task<FieldResult<ApiCollection<GamePlayer>, ApplicationError>> GetGamePlayersAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListPlayersQuery(gameId), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<ApiCollection<GamePlayer>>(result.Value)
            : result.Error!;
    }
}
