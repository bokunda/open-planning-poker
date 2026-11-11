namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer;

[MutationType]
public class GamePlayerMutations
{
    /// <summary>
    /// Join Game. Requires a valid user token.
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> JoinGameAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
        [Service] ICurrentUserProvider currentUserProvider,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if ((gameAccess.EnsureAuthenticated() ?? await gameAccess.EnsureGameExistsAsync(gameId, cancellationToken)) is { } accessError)
        {
            return accessError;
        }

        if (rateLimiter.TryAcquire(ActionRateLimiter.JoinGame, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        var user = await currentUserProvider.GetAsync(cancellationToken);
        if (!user.IsSuccess)
        {
            return user.Error!;
        }

        var result = await sender.Send(new JoinGameCommand(gameId, currentUserProvider.Id), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.SendAsync($"{nameof(JoinGameAsync)}_{gameId}", user.Value!, cancellationToken);
        return result.Value;
    }

    /// <summary>
    /// Leave a Game. Requires a valid user token.
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> LeaveGameAsync(
        [Service] ISender sender,
        [Service] ICurrentUserProvider currentUserProvider,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError)
        {
            return accessError;
        }

        var user = await currentUserProvider.GetAsync(cancellationToken);
        var result = await sender.Send(new LeaveGameCommand(gameId, currentUserProvider.Id), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        if (user.IsSuccess)
        {
            await eventSender.SendAsync($"{nameof(LeaveGameAsync)}_{gameId}", user.Value!, cancellationToken);
        }

        return result.Value;
    }
}
