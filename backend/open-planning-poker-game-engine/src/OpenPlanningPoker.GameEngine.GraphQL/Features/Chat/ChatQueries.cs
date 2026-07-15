using System.Text.Json;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Chat;

[QueryType]
public class ChatQueries
{
    /// <summary>
    /// Returns recent chat messages for a game (from Redis, 24h TTL).
    /// </summary>
    public async Task<List<ChatMessage>> GetChatMessagesAsync(
        [Service] IConnectionMultiplexer redis,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = $"chat:{gameId}";
        var values = await db.ListRangeAsync(key);

        var messages = new List<ChatMessage>();
        foreach (var value in values)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<ChatMessage>(value!);
                if (msg != null) messages.Add(msg);
            }
            catch { /* skip malformed */ }
        }

        // Return in chronological order (oldest first)
        messages.Reverse();
        return messages;
    }
}
