using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.Votes;

public sealed class Vote : Entity<Guid>
{
    internal Vote(Guid playerId, Guid ticketId, string value)
    {
        Id = SequentialGuidGenerator.Generate();
        PlayerId = playerId;
        TicketId = ticketId;
        Value = value;
    }

    public Guid PlayerId { get; set; }
    public Guid TicketId { get; set; }
    public string Value { get; set; }

    public const int MaxNoteLength = 200;

    /// <summary>Why the player chose this card, written after the reveal (OPP-122); null when none.</summary>
    public string? Note { get; private set; }

    /// <summary>How sure the player is about the agreed estimate, 1 to 5 (OPP-124); null when not given.</summary>
    public int? Confidence { get; private set; }

    public Ticket Ticket { get; set; } = null!;

    public static Vote Create(Guid playerId, Guid ticketId, string value)
    {
        var vote = new Vote(playerId, ticketId, value);
        vote.RaiseDomainEvent(new CreateVoteDomainEvent(vote.Id));
        return vote;
    }

    public void SetNote(string? note) => Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    public void SetConfidence(int? confidence) => Confidence = confidence;

    public void Update(string value)
    {
        Value = value;

        SetCreated(DateTimeOffset.UtcNow, CreatedBy);
        RaiseDomainEvent(new UpdateVoteDomainEvent(Id));
    }
}