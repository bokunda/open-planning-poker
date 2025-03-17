namespace OpenPlanningPoker.Shared.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithMetrics(opt =>
                opt
                    .SetResourceBuilder(ResourceBuilder
                        .CreateDefault()
                        .AddService(configuration["Otel:ServiceName"]!))
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(configuration["Otel:Endpoint"]!);
                    })
        );
        return services;
    }
}
