namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

[MutationType]
public class GameMutations
{
    /// <summary>
    /// Creates a game, returns game details.
    /// </summary>
    public async Task<FieldResult<Game, ApplicationError>> CreateGameAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] string name,
        [Required] string description,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateGameCommand(name, description), cancellationToken);

        if (result.IsSuccess)
        {
            await sender.Send(new CreateGameSettingsCommand(
                result.Value!.Id, 
                GameSettingsConstants.FibonacciDeckSetup), 
                cancellationToken);
        }

        return result.IsSuccess
            ? mapper.Map<Game>(result.Value!)
            : result.Error!;
    }

    public async Task<FieldResult<GameReport, ApplicationError>> GenerateGameReport(
        [Service] ISender sender,
        [Required] Guid gameId,
        CancellationToken cancellationToken)
    {
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
