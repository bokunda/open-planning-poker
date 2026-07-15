using System.Text.Json;
using HotChocolate;
using OpenPlanningPoker.Shared.Identity;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Chat;

[MutationType]
public class ChatMutations
{
    private const int MaxMessages = 100;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    /// <summary>
    /// Sends a chat message to a game.
    /// </summary>
    public async Task<FieldResult<ChatMessage, ApplicationError>> SendChatMessageAsync(
        [Service] ICurrentUserProvider currentUserProvider,
        [Service] ITopicEventSender eventSender,
        [Service] IConnectionMultiplexer redis,
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

        // Store in Redis with 24h TTL
        var db = redis.GetDatabase();
        var key = $"chat:{gameId}";
        var json = JsonSerializer.Serialize(message);
        await db.ListLeftPushAsync(key, json);
        await db.ListTrimAsync(key, 0, MaxMessages - 1);
        await db.KeyExpireAsync(key, Ttl);

        // Publish to subscribers
        await eventSender.SendAsync(
            $"{nameof(SendChatMessageAsync)}_{gameId}",
            message,
            cancellationToken);

        return message;
    }
}
