using HotChocolate;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Chat;

[SubscriptionType]
public class ChatSubscriptions
{
    [Subscribe]
    [Topic($"{nameof(ChatMutations.SendChatMessageAsync)}_{{{nameof(gameId)}}}")]
    public ChatMessage OnChatMessage(Guid gameId, [EventMessage] ChatMessage message) => message;
}
