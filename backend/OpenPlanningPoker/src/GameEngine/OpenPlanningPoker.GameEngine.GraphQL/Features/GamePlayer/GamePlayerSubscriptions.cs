namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer;

[SubscriptionType]
public class GamePlayerSubscriptions
{
    [Subscribe]
    [Topic($"{nameof(GamePlayerMutations.JoinGameAsync)}_{{{nameof(gameId)}}}")]

    public BaseUserProfile OnPlayerJoined(Guid gameId, [EventMessage] BaseUserProfile user) => user;

    [Subscribe]
    [Topic($"{nameof(GamePlayerMutations.LeaveGameAsync)}_{{{nameof(gameId)}}}")]
    public BaseUserProfile OnPlayerLeave(Guid gameId, [EventMessage] BaseUserProfile user) => user;
}
