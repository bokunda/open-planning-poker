using HotChocolate;
using OpenPlanningPoker.Shared.Identity;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Chat;

[MutationType]
public class ChatMutations
{
    /// <summary>
    /// Sends a chat message to a game.
    /// </summary>
    public async Task<FieldResult<ChatMessage, ApplicationError>> SendChatMessageAsync(
        [Service] ICurrentUserProvider currentUserProvider,
        [Service] ITopicEventSender eventSender,
        [Required] Guid gameId,
        [Required] string content,
        CancellationToken cancellationToken = default)
    {
        var user = await currentUserProvider.GetAsync(cancellationToken);

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            PlayerName = user.Value?.UserName ?? "Unknown",
            Content = content,
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        await eventSender.SendAsync(
            $"{nameof(SendChatMessageAsync)}_{gameId}",
            message,
            cancellationToken);

        return message;
    }
}
