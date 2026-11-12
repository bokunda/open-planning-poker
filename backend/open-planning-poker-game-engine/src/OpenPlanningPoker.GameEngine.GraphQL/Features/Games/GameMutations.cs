namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

[MutationType]
public class GameMutations
{
    /// <summary>
    /// Creates a game, returns game details. The caller becomes the game host.
    /// </summary>
    public async Task<FieldResult<Game, ApplicationError>> CreateGameAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] string name,
        [Required] string description,
        string? deckSetup,
        Guid? teamId,
        CancellationToken cancellationToken)
    {
        // The creator becomes the host, so an anonymous game would have no host.
        if (gameAccess.EnsureAuthenticated() is { } accessError)
        {
            return accessError;
        }

        // A game in a team room is one sprint of that team; only its members start one.
        if (teamId is { } team && await gameAccess.EnsureTeamMemberAsync(team, cancellationToken) is { } teamError)
        {
            return teamError;
        }

        if (rateLimiter.TryAcquire(ActionRateLimiter.CreateGame, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        var result = await sender.Send(new CreateGameCommand(name, description, gameAccess.CurrentUserId, teamId), cancellationToken);

        if (result.IsSuccess)
        {
            var deck = string.IsNullOrWhiteSpace(deckSetup)
                ? GameSettingsConstants.FibonacciDeckSetup
                : deckSetup;
            await sender.Send(new CreateGameSettingsCommand(
                result.Value!.Id,
                deck),
                cancellationToken);
        }

        return result.IsSuccess
            ? result.Value!.ToGraphQL()
            : result.Error!;
    }

    /// <summary>
    /// Deletes a game with its tickets, votes and players. Host only (not co-hosts).
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> DeleteGameAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureOwnerAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new DeleteGameCommand(gameId), cancellationToken);
        return result.IsSuccess
            ? result.Value
            : result.Error!;
    }

    /// <summary>
    /// Generates a PDF report of the game. Only players of the game can generate it.
    /// </summary>
    public async Task<FieldResult<GameReport, ApplicationError>> GenerateGameReport(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureMemberAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        if (rateLimiter.TryAcquire(ActionRateLimiter.GenerateReport, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        var result = await sender.Send(new CreateGameReportCommand(gameId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        return new GameReport
        {
            GameId = gameId,
            FileName = $"opp_game_export_{result.Value!.GameName.Replace(' ', '_')}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
            Data = result.Value!.Stream!.ToArray()
        };
    }
}
