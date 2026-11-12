namespace OpenPlanningPoker.GameEngine.Domain.Tickets;

public sealed class Ticket : Entity<Guid>
{
    public const int MaxFinalEstimateLength = 32;

    public Guid GameId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    /// <summary>
    /// While false, vote values on this ticket are visible only to the player who cast them.
    /// </summary>
    public bool VotesRevealed { get; private set; }

    /// <summary>The estimate the host settled on after the reveal; null until one is chosen.</summary>
    public string? FinalEstimate { get; private set; }

    /// <summary>Order of the ticket in the game's list (0 first). Tickets with the same value keep creation order.</summary>
    public int Position { get; private set; }

    public ICollection<Vote>? Votes;

    public Game Game { get; set; } = null!;

    internal Ticket(Guid gameId, string name, string description, int position)
    {
        GameId = gameId;
        Name = name;
        Description = description;
        Position = position;
    }

    public static Ticket Create(Guid gameId, string name, string description, int position = 0)
    {
        var ticket = new Ticket(gameId, name, description, position);
        ticket.RaiseDomainEvent(new CreateTicketDomainEvent(ticket.Id));
        return ticket;
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        RaiseDomainEvent(new UpdateTicketDomainEvent(Id));
    }

    public void RevealVotes() => VotesRevealed = true;

    /// <summary>A new voting round also drops the estimate chosen in the previous one.</summary>
    public void HideVotes()
    {
        VotesRevealed = false;
        FinalEstimate = null;
    }

    public void SetFinalEstimate(string? value) =>
        FinalEstimate = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void MoveTo(int position) => Position = position;
}
