namespace OpenPlanningPoker.GameEngine.GraphQL.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<OpenPlanningPokerGameEngineDbContext>();
        dbContext.Database.Migrate();
    }

    public static void UseCustomExceptionHandler(this IApplicationBuilder app) => 
        app.UseMiddleware<ExceptionHandlingMiddleware>();

    public const int MaxExecutionDepth = 10;

    public static IRequestExecutorBuilder AddGraphQLServices(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var isDevelopment = builder.Environment.IsDevelopment();

        return builder.AddGraphQL()
            .AddQueryConventions()
            .AddMutationConventions()
            // One shared, resilient Redis connection for subscriptions and chat history.
            .AddRedisSubscriptions(sp => sp.GetRequiredService<IConnectionMultiplexer>())
            .AddMaxExecutionDepthRule(MaxExecutionDepth)
            .ModifyRequestOptions(options =>
            {
                options.ExecutionTimeout = TimeSpan.FromSeconds(10);
                options.IncludeExceptionDetails = isDevelopment;
            })
            .DisableIntrospection(!isDevelopment)
            .AddTypes();
    }

    /// <summary>
    /// Single Redis connection. abortConnect=false keeps the app starting (and reconnecting in the
    /// background) when Redis is briefly unavailable instead of crashing on startup.
    /// </summary>
    public static IServiceCollection AddRedisConnection(this IServiceCollection services, IConfiguration configuration) =>
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration["ConnectionStrings:Cache"]!);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

    public static IHealthChecksBuilder AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration) =>
        services.AddHealthChecks()
            .AddNpgSql(configuration["ConnectionStrings:Database"]!)
            .AddCheck<CloudServiceHealthCheck>("CloudServiceProvider");
}
