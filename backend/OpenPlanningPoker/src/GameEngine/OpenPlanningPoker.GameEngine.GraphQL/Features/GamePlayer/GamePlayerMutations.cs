namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer;

[MutationType]
public class GamePlayerMutations
{
    /// <summary>
    /// Join Game
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> JoinGameAsync(
        [Service] ISender sender,
        [Service] ICurrentUserProvider currentUserProvider,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new JoinGameCommand(gameId, currentUserProvider.Id), cancellationToken);

        return result.IsSuccess
            ? result.Value
            : result.Error!;
    }

    /// <summary>
    /// Leave a Game
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> LeaveGameAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Service] ICurrentUserProvider currentUserProvider,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new LeaveGameCommand(gameId, currentUserProvider.Id), cancellationToken);
        return result.IsSuccess
            ? result.Value
            : result.Error!;
    }
}
