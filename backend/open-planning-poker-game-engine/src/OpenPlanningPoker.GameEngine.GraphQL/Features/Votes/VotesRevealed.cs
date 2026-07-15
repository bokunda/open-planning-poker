namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

public class VotesRevealed
{
    public Guid TicketId { get; set; }
    public string RevealedBy { get; set; } = string.Empty;
}
