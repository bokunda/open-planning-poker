using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;
using PlayerRole = OpenPlanningPoker.GameEngine.Domain.GamePlayer.PlayerRole;

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

        await eventSender.PublishAsync(gameId, GameChangeKind.Players, cancellationToken: cancellationToken);
        return result.Value;
    }

    /// <summary>
    /// Switches between voting (PLAYER) and watching (OBSERVER). Players change their own role;
    /// the host and co-hosts can change anyone's. Leave <c>playerId</c> empty for yourself.
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> SetPlayerRoleAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] PlayerRole role,
        Guid? playerId,
        CancellationToken cancellationToken = default)
    {
        var target = playerId ?? gameAccess.CurrentUserId;
        var accessError = target == gameAccess.CurrentUserId
            ? await gameAccess.EnsureMemberAsync(gameId, cancellationToken)
            : await gameAccess.EnsureHostAsync(gameId, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        var result = await sender.Send(new SetPlayerRoleCommand(gameId, target, role), cancellationToken);
        return await PublishPlayersAsync(eventSender, gameId, result, cancellationToken);
    }

    /// <summary>Makes a player co-host, or a regular player again. Host only.</summary>
    public async Task<FieldResult<bool, ApplicationError>> SetCoHostAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] Guid playerId,
        [Required] bool isCoHost,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureOwnerAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new SetCoHostCommand(gameId, playerId, isCoHost), cancellationToken);
        return await PublishPlayersAsync(eventSender, gameId, result, cancellationToken);
    }

    /// <summary>
    /// Removes a player from the game (their votes stay). Host and co-hosts; the host cannot be removed.
    /// The removed player can join again with the link.
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> RemovePlayerAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] Guid playerId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new RemovePlayerCommand(gameId, playerId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.PlayerRemoved, playerId: playerId, cancellationToken: cancellationToken);
        await eventSender.PublishAsync(gameId, GameChangeKind.Players, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>Hands the game over to another player in it. Host only; the old host stays as a player.</summary>
    public async Task<FieldResult<bool, ApplicationError>> TransferHostAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] Guid playerId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureOwnerAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new TransferHostCommand(gameId, playerId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Game, cancellationToken: cancellationToken);
        await eventSender.PublishAsync(gameId, GameChangeKind.Players, cancellationToken: cancellationToken);
        return true;
    }

    private static async Task<FieldResult<bool, ApplicationError>> PublishPlayersAsync(
        ITopicEventSender eventSender, Guid gameId, OpenPlanningPoker.Shared.Results.Result<bool, ApplicationError> result, CancellationToken cancellationToken)
    {
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Players, cancellationToken: cancellationToken);
        return true;
    }
}
