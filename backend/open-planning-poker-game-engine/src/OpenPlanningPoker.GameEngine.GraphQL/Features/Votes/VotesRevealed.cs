namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

public class VotesRevealed
{
    public Guid TicketId { get; set; }
    public string RevealedBy { get; set; } = string.Empty;

    /// <summary>
    /// All votes of the ticket with their values, sent once the host reveals them.
    /// </summary>
    public List<Vote> Votes { get; set; } = [];
}
