namespace OpenPlanningPoker.GameEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        AddPersistence(services, configuration);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Database") ??
            throw new ArgumentNullException(nameof(configuration));

        services.AddDbContext<OpenPlanningPokerGameEngineDbContext>(options =>
        {
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IGamePlayerRepository, GamePlayerRepository>();
        services.AddScoped<IGameSettingsRepository, GameSettingsRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();


        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OpenPlanningPokerGameEngineDbContext>());
    }
}