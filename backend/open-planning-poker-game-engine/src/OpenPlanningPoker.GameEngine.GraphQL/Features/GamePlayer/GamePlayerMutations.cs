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
        [Service] ITopicEventSender eventSender,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var user = await currentUserProvider.GetAsync(cancellationToken);
        var result = await sender.Send(new JoinGameCommand(gameId, currentUserProvider.Id), cancellationToken);
        await eventSender.SendAsync($"{nameof(JoinGameAsync)}_{gameId}", user.Value, cancellationToken);

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
        [Service] ITopicEventSender eventSender,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var user = await currentUserProvider.GetAsync(cancellationToken);
        var result = await sender.Send(new LeaveGameCommand(gameId, currentUserProvider.Id), cancellationToken);
        await eventSender.SendAsync($"{nameof(LeaveGameAsync)}_{gameId}", user, cancellationToken);

        return result.IsSuccess
            ? result.Value
            : result.Error!;
    }
}
