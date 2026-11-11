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
        return result.IsSuccess
            ? result.Value!.ToGraphQL()
            : result.Error!;
    }
}
