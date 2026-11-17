namespace OpenPlanningPoker.GameEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        AddPersistence(services, configuration);
        AddIntegrations(services, configuration);

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
        services.AddScoped<ITeamRepository, TeamRepository>();


        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OpenPlanningPokerGameEngineDbContext>());
    }

    /// <summary>Jira (OPP-10). The keys that encrypt the tokens live in the database, next to the tokens' owners.</summary>
    private static void AddIntegrations(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Application.Features.Integrations.Jira.JiraOptions>(configuration.GetSection(Application.Features.Integrations.Jira.JiraOptions.Section));
        Microsoft.AspNetCore.DataProtection.DataProtectionBuilderExtensions.SetApplicationName(
            Microsoft.AspNetCore.DataProtection.EntityFrameworkCoreDataProtectionExtensions.PersistKeysToDbContext<OpenPlanningPokerGameEngineDbContext>(
                services.AddDataProtection()),
            "OpenPlanningPoker.GameEngine");
        services.AddScoped<Domain.Integrations.IJiraConnectionRepository, JiraConnectionRepository>();
        services.AddScoped<Application.Features.Integrations.Jira.ISecretProtector, Integrations.DataProtectionSecretProtector>();
        services.AddHttpClient<Application.Features.Integrations.Jira.IJiraApi, Integrations.JiraApiClient>(client =>
            client.Timeout = TimeSpan.FromSeconds(20));
        services.AddScoped<Application.Features.Integrations.Jira.JiraService>();

        // GitHub, Linear and Azure DevOps (OPP-116): each stays off until its OAuth app is configured.
        services.Configure<Application.Features.Integrations.Trackers.GitHubOptions>(configuration.GetSection(Application.Features.Integrations.Trackers.GitHubOptions.Section));
        services.Configure<Application.Features.Integrations.Trackers.LinearOptions>(configuration.GetSection(Application.Features.Integrations.Trackers.LinearOptions.Section));
        services.Configure<Application.Features.Integrations.Trackers.AzureDevOpsOptions>(configuration.GetSection(Application.Features.Integrations.Trackers.AzureDevOpsOptions.Section));
        services.AddScoped<Domain.Integrations.ITrackerConnectionRepository, TrackerConnectionRepository>();
        services.AddHttpClient<Integrations.GitHubTrackerClient>(client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddHttpClient<Integrations.LinearTrackerClient>(client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddHttpClient<Integrations.AzureDevOpsTrackerClient>(client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddScoped<Application.Features.Integrations.Trackers.ITrackerClient>(sp => sp.GetRequiredService<Integrations.GitHubTrackerClient>());
        services.AddScoped<Application.Features.Integrations.Trackers.ITrackerClient>(sp => sp.GetRequiredService<Integrations.LinearTrackerClient>());
        services.AddScoped<Application.Features.Integrations.Trackers.ITrackerClient>(sp => sp.GetRequiredService<Integrations.AzureDevOpsTrackerClient>());
        services.AddScoped<Application.Features.Integrations.Trackers.TrackerService>();

        // Estimation insights (OPP-117): cycle times from Jira and the session summary webhook.
        services.AddScoped<ITicketCycleTimeRepository, TicketCycleTimeRepository>();
        services.AddHttpClient<Application.Features.Teams.IWebhookPoster, Integrations.WebhookPoster>(client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<Application.Features.Teams.InsightsService>();

        // Slack and Teams (OPP-22, OPP-23).
        services.Configure<Application.Features.Integrations.Chat.ChatOptions>(configuration.GetSection(Application.Features.Integrations.Chat.ChatOptions.Section));
        services.AddScoped<Domain.Integrations.IChatIntegrationRepository, ChatIntegrationRepository>();
        services.AddHttpClient<Application.Features.Integrations.Chat.ISlackApi, Integrations.SlackApiClient>(client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<Application.Features.Integrations.Chat.ChatGameService>();
    }
}