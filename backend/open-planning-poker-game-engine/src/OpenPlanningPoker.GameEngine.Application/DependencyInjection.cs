namespace OpenPlanningPoker.GameEngine.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddRedis(configuration);
        CacheExtensions.AddHybridCache(services, true);
        services.AddScoped<IGameAccessService, GameAccessService>();
        services.AddScoped<Features.Teams.Planning.Loader>();
        services.AddScoped<Features.Teams.RetroService>();
        services.AddScoped<Features.Teams.TeamApiService>();
        services.AddScoped<Features.Teams.OrganizationService>();

        return services;
    }
}