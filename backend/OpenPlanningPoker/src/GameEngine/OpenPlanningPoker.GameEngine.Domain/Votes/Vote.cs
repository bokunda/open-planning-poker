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

    public Ticket Ticket { get; set; } = null!;

    public static Vote Create(Guid playerId, Guid ticketId, string value)
    {
        var vote = new Vote(playerId, ticketId, value);
        vote.RaiseDomainEvent(new CreateVoteDomainEvent(vote.Id));
        return vote;
    }

    public void Update(string value)
    {
        Value = value;

        SetCreated(DateTimeOffset.UtcNow, CreatedBy);
        RaiseDomainEvent(new UpdateVoteDomainEvent(Id));
    }
}