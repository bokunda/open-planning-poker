namespace OpenPlanningPoker.Shared.Application.Extensions;

public static class CacheExtensions
{
    private const int DefaultMaximumPayloadBytes = 1024 * 1024;
    private const int DefaultMaximumKeyLength = 1024;
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services)
        => services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = DefaultMaximumPayloadBytes;
            options.MaximumKeyLength = DefaultMaximumKeyLength;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(30),
                Expiration = TimeSpan.FromDays(1)
            };
        });
}
