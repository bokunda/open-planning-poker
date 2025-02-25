namespace OpenPlanningPoker.GameEngine.GraphQL.Extensions;

public static class KeyCloakExtensions
{
    private const string AdminSection = "KeycloakAdmin";
    private const string ProtectionClient = "protection";

    public static void AddKeyCloak(this IServiceCollection services, IConfigurationManager configuration)
    {
        services.AddKeycloakWebApiAuthentication(configuration);

        services
            .AddAuthorization()
            .AddKeycloakAuthorization(configuration)
            .AddAuthorizationServer(configuration);

        services
            .AddKeycloakAdminHttpClient(configuration, keycloakClientSectionName: AdminSection)
            .AddClientCredentialsTokenHandler(AdminSection);

        services.AddAccessTokenManagement(configuration);
    }

    private static void AddAccessTokenManagement(this IServiceCollection services, IConfigurationManager configuration)
    {
        var adminClientOptions = configuration.GetKeycloakOptions<KeycloakAdminClientOptions>(configSectionName: AdminSection)!;
        var protectionClientOptions = configuration.GetKeycloakOptions<KeycloakProtectionClientOptions>()!;

        services.AddSingleton(adminClientOptions);
        services.AddSingleton(protectionClientOptions);

        services.AddDistributedMemoryCache();
        services
            .AddClientCredentialsTokenManagement()
            .AddClient(
                AdminSection,
                client =>
                {
                    client.ClientId = adminClientOptions.Resource;
                    client.ClientSecret = adminClientOptions.Credentials.Secret;
                    client.TokenEndpoint = adminClientOptions.KeycloakTokenEndpoint;
                }
            )
            .AddClient(
                ProtectionClient,
                client =>
                {
                    client.ClientId = protectionClientOptions.Resource;
                    client.ClientSecret = protectionClientOptions.Credentials.Secret;
                    client.TokenEndpoint = protectionClientOptions.KeycloakTokenEndpoint;
                }
            );
    }
}