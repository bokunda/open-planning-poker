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

    public const int MaxExternalKeyLength = 64;
    public const int MaxExternalUrlLength = 512;

    /// <summary>Key of the issue in the tracker the ticket was imported from (e.g. Jira "OPP-118"); null otherwise.</summary>
    public string? ExternalKey { get; private set; }

    /// <summary>Link back to that issue.</summary>
    public string? ExternalUrl { get; private set; }

    /// <summary>Tracker of <see cref="ExternalKey"/>: "jira", "github", "linear" or "azure-devops" (OPP-130).</summary>
    public string? ExternalProvider { get; private set; }

    /// <summary>When the cards were last turned over; starts the discussion timer (OPP-123).</summary>
    public DateTimeOffset? RevealedAt { get; private set; }

    /// <summary>When the final estimate was chosen; with <see cref="RevealedAt"/> it gives the discussion time.</summary>
    public DateTimeOffset? EstimatedAt { get; private set; }

    /// <summary>How many times the cards were turned over for this ticket (1 = agreed on the first vote).</summary>
    public int VoteRounds { get; private set; }

    /// <summary>The team needs more information before it can estimate this ticket (OPP-126).</summary>
    public bool NeedsInfo { get; private set; }

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

    public void RevealVotes()
    {
        VotesRevealed = true;
        RevealedAt = DateTimeOffset.UtcNow;
        VoteRounds++;
    }

    /// <summary>A new voting round also drops the estimate chosen in the previous one.</summary>
    public void HideVotes()
    {
        VotesRevealed = false;
        FinalEstimate = null;
        RevealedAt = null;
        EstimatedAt = null;
    }

    public void SetFinalEstimate(string? value)
    {
        FinalEstimate = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        EstimatedAt = FinalEstimate is null ? null : DateTimeOffset.UtcNow;
        if (FinalEstimate is not null)
        {
            NeedsInfo = false;
        }
    }

    public void MarkNeedsInfo(bool needsInfo) => NeedsInfo = needsInfo;

    public void MoveTo(int position) => Position = position;

    /// <summary>Remembers the tracker issue this ticket came from (OPP-11).</summary>
    public void LinkTo(string key, string url, string provider = "jira")
    {
        ExternalKey = key;
        ExternalUrl = url;
        ExternalProvider = provider;
    }
}
