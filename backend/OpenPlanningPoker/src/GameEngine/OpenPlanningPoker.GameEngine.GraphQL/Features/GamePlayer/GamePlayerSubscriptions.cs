namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer;

[SubscriptionType]
public class GamePlayerSubscriptions
{
    [Subscribe]
    [Topic(nameof(GamePlayerMutations.JoinGameAsync))]
    public Guid OnPlayerJoined([EventMessage] Guid userId) => userId;

    [Subscribe]
    [Topic(nameof(GamePlayerMutations.LeaveGameAsync))]
    public Guid OnPlayerLeave([EventMessage] Guid userId) => userId;
}
