namespace OpenPlanningPoker.GameEngine.Domain.Tickets;

public sealed class Ticket : Entity<Guid>
{
    public Guid GameId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    public ICollection<Vote>? Votes;

    public Game Game { get; set; } = null!;

    internal Ticket(Guid gameId, string name, string description)
    {
        GameId = gameId;
        Name = name;
        Description = description;
    }

    public static Ticket Create(Guid gameId, string name, string description)
    {
        var ticket = new Ticket(gameId, name, description);
        ticket.RaiseDomainEvent(new CreateTicketDomainEvent(ticket.Id));
        return ticket;
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        RaiseDomainEvent(new UpdateTicketDomainEvent(Id));
    }
}