namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Settings;

public class Settings
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public string DeckSetup { get; set; } = string.Empty;
}
