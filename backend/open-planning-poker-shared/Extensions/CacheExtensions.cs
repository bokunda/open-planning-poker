namespace OpenPlanningPoker.Shared.Extensions;

public static class CacheExtensions
{
    private const int DefaultMaximumPayloadBytes = 1024 * 1024;
    private const int DefaultMaximumKeyLength = 1024;
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services, bool disableLocalCache = false, TimeSpan? defaultExpiration = null)
        => services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = DefaultMaximumPayloadBytes;
            options.MaximumKeyLength = DefaultMaximumKeyLength;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Flags = disableLocalCache ? HybridCacheEntryFlags.DisableLocalCache : HybridCacheEntryFlags.None,
                Expiration = defaultExpiration ?? TimeSpan.FromDays(1)
            };
        });
}
