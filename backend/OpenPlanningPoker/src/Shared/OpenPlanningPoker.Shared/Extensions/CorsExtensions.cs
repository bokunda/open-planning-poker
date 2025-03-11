namespace OpenPlanningPoker.Shared.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCors(this IServiceCollection services, string policyName, string[] origins, bool allowAll = false)
    {
        if (allowAll)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(policyName, builder =>
                {
                    builder.SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            return services;
        }

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, builder =>
            {
                builder.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
