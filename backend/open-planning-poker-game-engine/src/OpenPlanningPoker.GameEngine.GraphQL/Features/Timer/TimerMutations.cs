using System.Text.Json;
using HotChocolate;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Timer;

[MutationType]
public class TimerMutations
{
    public const int MinDurationSeconds = 10;
    public const int MaxDurationSeconds = 3600;

    /// <summary>
    /// Starts (or restarts) the voting timer for everyone in the game. Host only.
    /// </summary>
    public async Task<FieldResult<GameTimer, ApplicationError>> StartTimerAsync(
        [Service] IGameAccessService gameAccess,
        [Service] ITopicEventSender eventSender,
        [Service] IConnectionMultiplexer redis,
        [Required] Guid gameId,
        [Required] int durationSeconds,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        if (durationSeconds < MinDurationSeconds || durationSeconds > MaxDurationSeconds)
        {
            return new ApplicationError("InvalidTimerDuration", $"Duration must be between {MinDurationSeconds} and {MaxDurationSeconds} seconds.");
        }

        var now = DateTime.UtcNow;
        var timer = new GameTimer
        {
            GameId = gameId,
            DurationSeconds = durationSeconds,
            Running = true,
            EndsAt = now.AddSeconds(durationSeconds),
            ServerTime = now,
        };
        await SaveAndPublishAsync(redis, eventSender, timer, cancellationToken);
        return timer;
    }

    /// <summary>
    /// Stops the voting timer for everyone in the game. Host only.
    /// </summary>
    public async Task<FieldResult<GameTimer, ApplicationError>> StopTimerAsync(
        [Service] IGameAccessService gameAccess,
        [Service] ITopicEventSender eventSender,
        [Service] IConnectionMultiplexer redis,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var current = await TimerStore.GetAsync(redis, gameId);
        var timer = new GameTimer
        {
            GameId = gameId,
            DurationSeconds = current?.DurationSeconds ?? 60,
            Running = false,
            EndsAt = null,
            ServerTime = DateTime.UtcNow,
        };
        await SaveAndPublishAsync(redis, eventSender, timer, cancellationToken);
        return timer;
    }

    private static async Task SaveAndPublishAsync(IConnectionMultiplexer redis, ITopicEventSender eventSender, GameTimer timer, CancellationToken cancellationToken)
    {
        await TimerStore.SaveAsync(redis, timer);
        await eventSender.SendAsync(TimerStore.Topic(timer.GameId), timer, cancellationToken);
    }
}

/// <summary>Timer state in Redis (key <c>timer:{gameId}</c>), kept for a day after the last change.</summary>
internal static class TimerStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    public static string Topic(Guid gameId) => $"TimerChanged_{gameId}";

    public static async Task SaveAsync(IConnectionMultiplexer redis, GameTimer timer) =>
        await redis.GetDatabase().StringSetAsync($"timer:{timer.GameId}", JsonSerializer.Serialize(timer), Ttl);

    public static async Task<GameTimer?> GetAsync(IConnectionMultiplexer redis, Guid gameId)
    {
        var value = await redis.GetDatabase().StringGetAsync($"timer:{gameId}");
        if (value.IsNullOrEmpty) return null;
        try { return JsonSerializer.Deserialize<GameTimer>((string)value!); }
        catch (JsonException) { return null; }
    }
}
