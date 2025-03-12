namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

public sealed class Game
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public async Task<FieldResult<Settings.Settings, ApplicationError>> GetSettingsDetailsAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetGameSettingsQuery(Id), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Settings.Settings>(result.Value)
            : result.Error!;
    }
}
