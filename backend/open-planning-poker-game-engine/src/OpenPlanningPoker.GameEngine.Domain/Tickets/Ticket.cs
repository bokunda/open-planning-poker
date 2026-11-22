using OpenPlanningPoker.GameEngine.Domain.Tickets.Events;
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

    public const int MaxClarityLength = 600;
    public const int MaxSummaryLength = 300;

    /// <summary>Points of the AI clarity check (OPP-150), one per line; null until someone runs it.</summary>
    public string? ClarityNotes { get; private set; }

    /// <summary>One sentence about the discussion (OPP-152), written or edited by the host; goes to the tracker comment and the report.</summary>
    public string? DiscussionSummary { get; private set; }

    public void SetClarityNotes(IEnumerable<string>? notes)
    {
        var text = notes is null ? null : string.Join('\n', notes.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()));
        ClarityNotes = string.IsNullOrEmpty(text) ? null : text.Length > MaxClarityLength ? text[..MaxClarityLength] : text;
    }

    public void SetDiscussionSummary(string? summary)
    {
        var text = summary?.Trim();
        DiscussionSummary = string.IsNullOrEmpty(text) ? null : text.Length > MaxSummaryLength ? text[..MaxSummaryLength] : text;
    }

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

    /// <summary>Card column of the ticket on the sorting board; null while it is unsorted (OPP-142).</summary>
    public string? SortColumn { get; private set; }

    /// <summary>How many times the ticket was moved on the sorting board since the mark was last cleared.</summary>
    public int SortMoves { get; private set; }

    /// <summary>The team keeps moving this ticket, so it needs a talk (OPP-143).</summary>
    public bool SortContested { get; private set; }

    /// <summary>Player who moved the ticket last; a different player moving it again means disagreement.</summary>
    public Guid? SortLastMovedBy { get; private set; }

    /// <summary>Chosen for the sprint in the team's sprint plan (OPP-148).</summary>
    public bool Planned { get; private set; }

    public void SetPlanned(bool planned) => Planned = planned;

    /// <summary>Has an estimate: the cards were turned over, or a final estimate was set (also from the sorting board).</summary>
    public bool IsEstimated => VotesRevealed || FinalEstimate is not null;

    /// <summary>A ticket moved this many times is contested.</summary>
    public const int ContestedAfterMoves = 3;

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
        var before = FinalEstimate;
        FinalEstimate = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (FinalEstimate is not null && FinalEstimate != before)
        {
            RaiseDomainEvent(new TicketEstimatedDomainEvent(Id, GameId, before is null));
        }
        EstimatedAt = FinalEstimate is null ? null : DateTimeOffset.UtcNow;
        if (FinalEstimate is not null)
        {
            NeedsInfo = false;
            // A ticket sent from the sorting board to a vote lands in the column of the agreed card.
            if (SortColumn is not null || SortContested)
            {
                SortColumn = FinalEstimate;
                SortContested = false;
            }
        }
    }

    /// <summary>
    /// Moves the ticket to a card column of the sorting board (null = back to unsorted). Returns false when it is
    /// already there. The ticket becomes contested after <see cref="ContestedAfterMoves"/> moves, or when another
    /// player moves it out of a column someone else chose.
    /// </summary>
    public bool MoveOnBoard(string? column, Guid playerId)
    {
        column = string.IsNullOrWhiteSpace(column) ? null : column.Trim();
        if (column == SortColumn)
        {
            return false;
        }

        var disagrees = SortColumn is not null && SortLastMovedBy is { } last && last != playerId;
        SortColumn = column;
        SortMoves++;
        SortLastMovedBy = playerId;
        if (disagrees || SortMoves >= ContestedAfterMoves)
        {
            SortContested = true;
        }

        return true;
    }

    /// <summary>The team agreed on the column: clears the mark and starts counting moves again.</summary>
    public void ClearContested()
    {
        SortContested = false;
        SortMoves = 0;
        SortLastMovedBy = null;
    }

    /// <summary>Turns the board column into the final estimate (OPP-144). False when there is nothing to apply.</summary>
    public bool ApplySortedEstimate()
    {
        if (SortColumn is null || SortContested || FinalEstimate is not null)
        {
            return false;
        }

        FinalEstimate = SortColumn;
        EstimatedAt = DateTimeOffset.UtcNow;
        NeedsInfo = false;
        RaiseDomainEvent(new TicketEstimatedDomainEvent(Id, GameId, true));
        return true;
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

/// <summary>
/// How long the tracker issue of a ticket took, read from Jira (OPP-135): first status change to done. Cached and
/// refreshed at most once a day.
/// </summary>
public sealed class TicketCycleTime
{
    private TicketCycleTime(Guid ticketId) => TicketId = ticketId;

    public Guid TicketId { get; private set; }
    public DateTimeOffset? StartedOn { get; private set; }
    public DateTimeOffset? DoneOn { get; private set; }
    public DateTimeOffset CheckedOn { get; private set; }

    public static TicketCycleTime Create(Guid ticketId) => new(ticketId);

    public void Update(DateTimeOffset? startedOn, DateTimeOffset? doneOn)
    {
        StartedOn = startedOn;
        DoneOn = doneOn;
        CheckedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>Days from start to done; null until the issue is done.</summary>
    public double? Days => StartedOn is { } s && DoneOn is { } d && d >= s ? Math.Round((d - s).TotalDays, 1) : null;
}

public interface ITicketCycleTimeRepository
{
    Task<IReadOnlyList<TicketCycleTime>> GetAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken = default);
    void Add(TicketCycleTime cycleTime);
}
