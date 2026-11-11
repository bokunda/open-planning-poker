using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace OpenPlanningPoker.UserManagement.GraphQL.Extensions;

/// <summary>
/// Settings used both to issue tokens (RegisterUser) and to validate them.
/// </summary>
public sealed record JwtSettings(string Secret, string Issuer, string Audience, TimeSpan Lifetime)
{
    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Secret));
}

/// <summary>
/// JWT setup registered locally (not via the Shared NuGet package) so it can be changed
/// without publishing a new package version. Validation must stay in sync with Game Engine.
/// </summary>
public static class JwtAuthenticationExtensions
{
    private const int MinimumSecretLength = 32;
    private const string DefaultIssuer = "open-planning-poker";
    private const string DefaultAudience = "open-planning-poker";

    // Secrets that were ever committed to the public repository. Never accept them.
    private static readonly string[] KnownPublicSecrets = ["OpenPlanningPokerDevelopmentSecretKey123"];

    /// <param name="isSchemaExport">
    /// True when the app only exports its GraphQL schema (CI). No secret is available there,
    /// so a throwaway random key is used and the app never serves requests.
    /// </param>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, bool isSchemaExport = false)
    {
        var secret = isSchemaExport
            ? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
            : configuration["Authentication:Secret"];

        if (string.IsNullOrWhiteSpace(secret) || secret.Length < MinimumSecretLength)
        {
            throw new InvalidOperationException(
                $"Authentication:Secret must be configured (env var Authentication__Secret) and be at least {MinimumSecretLength} characters long.");
        }

        if (KnownPublicSecrets.Contains(secret))
        {
            throw new InvalidOperationException("Authentication:Secret uses a publicly known value. Configure a new secret.");
        }

        var settings = new JwtSettings(
            secret,
            configuration["Authentication:Issuer"] ?? DefaultIssuer,
            configuration["Authentication:Audience"] ?? DefaultAudience,
            TimeSpan.FromDays(1));

        services.AddSingleton(settings);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = settings.SigningKey,
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        return services;
    }
}
