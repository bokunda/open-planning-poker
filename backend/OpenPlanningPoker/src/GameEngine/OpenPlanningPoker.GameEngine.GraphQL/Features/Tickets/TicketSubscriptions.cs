namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer;

[SubscriptionType]
public class TicketSubscriptions
{
    [Subscribe]
    [Topic($"{nameof(TicketMutations.CreateTicketAsync)}_{{{nameof(gameId)}}}")]
    public Ticket OnTicketCreated(Guid gameId, [EventMessage] Ticket ticket) => ticket;
}
