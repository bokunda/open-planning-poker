namespace OpenPlanningPoker.Shared.Application.Extensions;

public static class CacheExtensions
{
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services)
        => services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024;
            options.MaximumKeyLength = 1024;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(30),
                Expiration = TimeSpan.FromDays(1)
            };
        });
}
