namespace OpenPlanningPoker.GameEngine.Domain.Votes;

public sealed class Vote : Entity<Guid>
{
    internal Vote(Guid playerId, Guid ticketId, int value)
    {
        PlayerId = playerId;
        TicketId = ticketId;
        Value = value;
    }

    public Guid PlayerId { get; set; }
    public Guid TicketId { get; set; }
    public int Value { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public static Vote Create(Guid playerId, Guid ticketId, int value)
    {
        var vote = new Vote(playerId, ticketId, value);
        vote.RaiseDomainEvent(new CreateVoteDomainEvent(vote.Id));
        return vote;
    }

    public void Update(int value)
    {
        Value = value;

        SetCreated(DateTimeOffset.UtcNow, CreatedBy);
        RaiseDomainEvent(new UpdateVoteDomainEvent(Id));
    }
}