using HotChocolate;
using HotChocolate.Execution;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Chat;

[SubscriptionType]
public class ChatSubscriptions
{
    /// <summary>New chat messages of a game. Only players of the game can subscribe.</summary>
    [Subscribe(With = nameof(SubscribeToChatAsync))]
    public ChatMessage OnChatMessage(Guid gameId, [EventMessage] ChatMessage message) => message;

    public async ValueTask<ISourceStream<ChatMessage>> SubscribeToChatAsync(
        Guid gameId,
        [Service] IGameAccessService gameAccess,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureMemberAsync(gameId, cancellationToken) is { } error)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(error.Message ?? error.Code).SetCode(error.Code).Build());
        }

        return await receiver.SubscribeAsync<ChatMessage>(ChatTopics.Messages(gameId), cancellationToken);
    }
}

public static class ChatTopics
{
    public static string Messages(Guid gameId) => $"{nameof(ChatMutations.SendChatMessageAsync)}_{gameId}";
}
