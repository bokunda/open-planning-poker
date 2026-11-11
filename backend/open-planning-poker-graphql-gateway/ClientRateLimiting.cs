using System.Threading.RateLimiting;

namespace OpenPlanningPoker.Fusion.Gateway;

/// <summary>
/// Per-client request limit. The client is identified by <c>X-Real-IP</c>, which nginx sets to the
/// caller's address (overwriting any client value). This is only trustworthy because the container
/// ports are bound to 127.0.0.1, so every external request passes through nginx.
/// </summary>
public static class ClientRateLimiting
{
    public const string ClientIpHeader = "X-Real-IP";

    public static string GetClientIp(HttpContext context) =>
        context.Request.Headers[ClientIpHeader].FirstOrDefault()
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    public static IServiceCollection AddClientRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Generous enough for a whole team behind one office NAT address (every vote makes each
        // client refetch tickets), strict enough to stop request floods.
        var tokenLimit = configuration.GetValue("RateLimiting:Burst", 200);
        var tokensPerTenSeconds = configuration.GetValue("RateLimiting:TokensPerTenSeconds", 100);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetTokenBucketLimiter(GetClientIp(context), _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = tokenLimit,
                    TokensPerPeriod = tokensPerTenSeconds,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true,
                    QueueLimit = 0
                }));
        });

        return services;
    }
}
