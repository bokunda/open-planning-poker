using System.Text.Json;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Chat;

[QueryType]
public class ChatQueries
{
    /// <summary>
    /// Returns recent chat messages for a game (from Redis, 24h TTL). Empty for callers who are not
    /// players of the game.
    /// </summary>
    public async Task<List<ChatMessage>> GetChatMessagesAsync(
        [Service] IGameAccessService gameAccess,
        [Service] IConnectionMultiplexer redis,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureMemberAsync(gameId, cancellationToken) is not null)
        {
            return [];
        }

        var db = redis.GetDatabase();
        var key = $"chat:{gameId}";
        var values = await db.ListRangeAsync(key);

        var messages = new List<ChatMessage>();
        foreach (var value in values)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<ChatMessage>((string)value!);
                if (msg != null) messages.Add(msg);
            }
            catch { /* skip malformed */ }
        }

        // Return in chronological order (oldest first)
        messages.Reverse();
        return messages;
    }
}
