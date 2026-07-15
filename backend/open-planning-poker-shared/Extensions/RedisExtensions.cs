using StackExchange.Redis;

namespace OpenPlanningPoker.Shared.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:Cache"]!;
        var multiplexer = ConnectionMultiplexer.Connect(connectionString);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
        });

        return services;
    }
}
