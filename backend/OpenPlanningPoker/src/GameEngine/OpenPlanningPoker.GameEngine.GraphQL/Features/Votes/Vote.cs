namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

public class Vote
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Value { get; set; } = string.Empty;
}
