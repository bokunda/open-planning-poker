using System.Threading.RateLimiting;

namespace OpenPlanningPoker.GameEngine.GraphQL.RateLimiting;

/// <summary>
/// Stricter per-user limits for actions that are expensive or easy to abuse.
/// Fixed windows per (action, user); idle partitions are cleaned up by the runtime limiter.
/// </summary>
public sealed class ActionRateLimiter : IDisposable
{
    public const string CreateGame = nameof(CreateGame);
    public const string GenerateReport = nameof(GenerateReport);
    public const string SendChatMessage = nameof(SendChatMessage);
    public const string CreateTicket = nameof(CreateTicket);
    public const string Vote = nameof(Vote);
    public const string JoinGame = nameof(JoinGame);

    private static readonly Dictionary<string, (int Permits, TimeSpan Window)> Policies = new()
    {
        [CreateGame] = (10, TimeSpan.FromMinutes(1)),
        [GenerateReport] = (5, TimeSpan.FromMinutes(1)),
        [SendChatMessage] = (30, TimeSpan.FromMinutes(1)),
        // Importing tickets creates them one request each.
        [CreateTicket] = (300, TimeSpan.FromMinutes(1)),
        [Vote] = (120, TimeSpan.FromMinutes(1)),
        [JoinGame] = (60, TimeSpan.FromMinutes(1)),
    };

    public static ApplicationError RateLimited => new("RateLimited", "Too many requests. Please wait a moment and try again.");

    private readonly PartitionedRateLimiter<(string Action, string Key)> _limiter =
        PartitionedRateLimiter.Create<(string Action, string Key), (string Action, string Key)>(partition =>
        {
            var (permits, window) = Policies[partition.Action];
            return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true
            });
        });

    /// <summary>Returns <c>null</c> when allowed, otherwise the error to return to the client.</summary>
    public ApplicationError? TryAcquire(string action, Guid userId)
    {
        using var lease = _limiter.AttemptAcquire((action, userId.ToString()));
        return lease.IsAcquired ? null : RateLimited;
    }

    public void Dispose() => _limiter.Dispose();
}
