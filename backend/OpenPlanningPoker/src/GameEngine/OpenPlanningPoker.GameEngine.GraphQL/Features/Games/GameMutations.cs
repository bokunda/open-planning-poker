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
        return result.IsSuccess
            ? mapper.Map<Game>(result.Value!)
            : result.Error!;
    }
}
