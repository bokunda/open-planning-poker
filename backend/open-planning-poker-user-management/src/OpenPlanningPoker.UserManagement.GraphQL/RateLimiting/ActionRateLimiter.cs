using System.Threading.RateLimiting;

namespace OpenPlanningPoker.UserManagement.GraphQL.RateLimiting;

/// <summary>
/// Stricter limits for abusable actions. Registration is anonymous, so it is keyed by client IP
/// (X-Real-IP, forwarded by the Fusion gateway); username changes are keyed by user id.
/// </summary>
public sealed class ActionRateLimiter : IDisposable
{
    public const string RegisterUser = nameof(RegisterUser);
    public const string ChangeUsername = nameof(ChangeUsername);

    private static readonly Dictionary<string, (int Permits, TimeSpan Window)> Policies = new()
    {
        // A whole team can join from one office address at once.
        [RegisterUser] = (30, TimeSpan.FromMinutes(1)),
        [ChangeUsername] = (10, TimeSpan.FromMinutes(1)),
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
    public ApplicationError? TryAcquire(string action, string key)
    {
        using var lease = _limiter.AttemptAcquire((action, key));
        return lease.IsAcquired ? null : RateLimited;
    }

    public void Dispose() => _limiter.Dispose();
}
