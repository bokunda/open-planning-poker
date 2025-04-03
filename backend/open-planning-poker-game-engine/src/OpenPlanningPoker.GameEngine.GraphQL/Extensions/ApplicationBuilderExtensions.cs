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

    public static IRequestExecutorBuilder AddGraphQLServices(this WebApplicationBuilder builder, IConfiguration configuration) =>
        builder.AddGraphQL()
            //.AddAuthorization()
            .AddQueryConventions()
            .AddMutationConventions()
            .AddRedisSubscriptions((sp) => ConnectionMultiplexer.Connect(configuration["ConnectionStrings:Cache"]!))
            .AddTypes();

    public static IHealthChecksBuilder AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration) =>
        services.AddHealthChecks()
            .AddNpgSql(configuration["ConnectionStrings:Database"]!)
            .AddCheck<CloudServiceHealthCheck>("CloudServiceProvider");
}
