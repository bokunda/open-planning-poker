namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
