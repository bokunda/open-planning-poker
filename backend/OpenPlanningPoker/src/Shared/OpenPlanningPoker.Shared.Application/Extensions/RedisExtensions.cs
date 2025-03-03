namespace OpenPlanningPoker.Shared.Application.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration) => 
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["ConnectionStrings:Cache"];
        });
}
