using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Settings;

[MutationType]
public class SettingsMutations
{
    /// <summary>
    /// Creates Game Settings. Host only.
    /// </summary>
    public async Task<FieldResult<Settings, ApplicationError>> CreateSettingsAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        string? deckSetup,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        deckSetup = string.IsNullOrEmpty(deckSetup) ? GameSettingsConstants.FibonacciDeckSetup : deckSetup;
        var result = await sender.Send(new CreateGameSettingsCommand(gameId, deckSetup), cancellationToken);
        return result.IsSuccess
            ? result.Value!.ToGraphQL()
            : result.Error!;
    }

    /// <summary>
    /// Updates Game Settings. Host only.
    /// </summary>
    public async Task<FieldResult<Settings, ApplicationError>> UpdateSettingsAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid id,
        [Required] Guid gameId,
        string? deckSetup,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        deckSetup = string.IsNullOrEmpty(deckSetup) ? GameSettingsConstants.FibonacciDeckSetup : deckSetup;
        var result = await sender.Send(new UpdateGameSettingsCommand(id, gameId, deckSetup), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Game, cancellationToken: cancellationToken);
        return result.Value!.ToGraphQL();
    }

    /// <summary>
    /// Room options: auto-reveal, and the async voting window (players vote on all tickets on their own
    /// until <c>asyncVotingEndsAt</c>; null or a past time switches to meeting mode). Host and co-hosts.
    /// </summary>
    public async Task<FieldResult<Settings, ApplicationError>> UpdateGameOptionsAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] bool autoReveal,
        DateTimeOffset? asyncVotingEndsAt,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new UpdateGameOptionsCommand(gameId, autoReveal, asyncVotingEndsAt), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Game, cancellationToken: cancellationToken);
        return result.Value!.ToGraphQL();
    }
}
