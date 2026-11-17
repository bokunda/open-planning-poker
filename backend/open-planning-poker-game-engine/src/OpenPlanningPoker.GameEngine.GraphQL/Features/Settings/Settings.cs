namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Settings;

public class Settings
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public string DeckSetup { get; set; } = string.Empty;

    /// <summary>Cards are revealed as soon as every player (not observers) has voted.</summary>
    public bool AutoReveal { get; set; }

    /// <summary>End of the async voting window; null when the room is in meeting mode.</summary>
    public DateTimeOffset? AsyncVotingEndsAt { get; set; }

    /// <summary>Discussion options of the room (OPP-114).</summary>
    public async Task<Facilitation.FacilitationSettings> GetFacilitation([Service] ISender sender, CancellationToken cancellationToken) =>
        Facilitation.FacilitationSettings.From(await sender.Send(
            new Application.Features.Facilitation.GetFacilitationOptionsQuery(GameId), cancellationToken));
}
