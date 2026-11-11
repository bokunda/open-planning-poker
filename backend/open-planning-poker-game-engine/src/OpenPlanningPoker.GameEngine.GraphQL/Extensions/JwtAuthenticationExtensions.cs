using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace OpenPlanningPoker.GameEngine.GraphQL.Extensions;

/// <summary>
/// JWT validation registered locally (not via the Shared NuGet package) so it can be changed
/// without publishing a new package version. Must stay in sync with User Management.
/// </summary>
public static class JwtAuthenticationExtensions
{
    private const int MinimumSecretLength = 32;
    private const string DefaultIssuer = "open-planning-poker";
    private const string DefaultAudience = "open-planning-poker";

    // Secrets that were ever committed to the public repository. Never accept them.
    private static readonly string[] KnownPublicSecrets = ["OpenPlanningPokerDevelopmentSecretKey123"];

    /// <summary>Registers JWT bearer authentication with the shared secret, issuer and audience.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration with <c>Authentication:Secret</c> (and optional issuer/audience).</param>
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

        var issuer = configuration["Authentication:Issuer"] ?? DefaultIssuer;
        var audience = configuration["Authentication:Audience"] ?? DefaultAudience;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        return services;
    }
}
