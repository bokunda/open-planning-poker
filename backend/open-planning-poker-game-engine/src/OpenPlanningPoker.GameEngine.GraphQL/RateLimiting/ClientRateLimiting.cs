using System.Threading.RateLimiting;

namespace OpenPlanningPoker.GameEngine.GraphQL.RateLimiting;

/// <summary>
/// Per-client request limit for traffic that reaches this service directly through nginx
/// (WebSocket subscriptions on ge.openplanningpoker.com). Requests from the Fusion gateway are
/// already limited there and carry no <c>X-Forwarded-For</c>, so they are not counted twice.
/// The client is identified by <c>X-Real-IP</c>, set by nginx (or forwarded by the gateway); this is
/// trustworthy only because the container port is bound to 127.0.0.1.
/// </summary>
public static class ClientRateLimiting
{
    public const string ClientIpHeader = "X-Real-IP";
    private const string ForwardedForHeader = "X-Forwarded-For";

    public const string PublicApiPolicy = "public-api";
    public const string SiteVisitPolicy = "site-visit";

    /// <summary>The token of a public API request, hashed so it is never kept in memory as is; the address without one.</summary>
    private static string TokenKey(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) && header.Length > 7
            ? Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(header[7..])))[..16]
            : GetClientIp(context);
    }

    public static string GetClientIp(HttpContext? context) =>
        context?.Request.Headers[ClientIpHeader].FirstOrDefault()
        ?? context?.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    public static IServiceCollection AddClientRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var tokenLimit = configuration.GetValue("RateLimiting:Burst", 200);
        var tokensPerTenSeconds = configuration.GetValue("RateLimiting:TokensPerTenSeconds", 100);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                !context.Request.Headers.ContainsKey(ForwardedForHeader)
                    ? RateLimitPartition.GetNoLimiter("internal")
                    : RateLimitPartition.GetTokenBucketLimiter(GetClientIp(context), _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = tokenLimit,
                        TokensPerPeriod = tokensPerTenSeconds,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        AutoReplenishment = true,
                        QueueLimit = 0
                    }));

            // Public API (OPP-159): 60 requests a minute per token, on top of the limit per address.
            options.AddPolicy(PublicApiPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                "token:" + TokenKey(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            // Website page views (OPP-181): a visitor opens a few pages a minute.
            options.AddPolicy(SiteVisitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                "visit:" + GetClientIp(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                return ValueTask.CompletedTask;
            };
        });

        return services;
    }
}
