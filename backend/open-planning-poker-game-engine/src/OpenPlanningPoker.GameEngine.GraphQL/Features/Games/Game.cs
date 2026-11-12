namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

public sealed class Game
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Id of the player who runs the game: the creator until the host hands it over.
    /// The host and co-hosts manage tickets, settings and reveals.
    /// </summary>
    public Guid HostId { get; set; }

    /// <summary>Team room the game belongs to (a sprint of that team), if any.</summary>
    public Guid? TeamId { get; set; }

    public async Task<FieldResult<Settings.Settings, ApplicationError>> GetSettingsDetailsAsync(
            [Service] ISender sender,
            CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetGameSettingsQuery(Id), cancellationToken);
        return result.IsSuccess
            ? result.Value!.ToGraphQL()
            : result.Error!;
    }
}
