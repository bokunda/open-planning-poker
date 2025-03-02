namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Settings;

public class Settings
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public int VotingTime { get; set; }
    public bool IsBreakAllowed { get; set; }
}
