namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer;

[SubscriptionType]
public class GamePlayerSubscriptions
{
    [Subscribe]
    [Topic(nameof(GamePlayerMutations.JoinGameAsync))]
    public BaseUserProfile OnPlayerJoined([EventMessage] BaseUserProfile user) => user;

    [Subscribe]
    [Topic(nameof(GamePlayerMutations.LeaveGameAsync))]
    public BaseUserProfile OnPlayerLeave([EventMessage] BaseUserProfile user) => user;
}
