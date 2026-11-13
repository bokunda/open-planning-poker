using System.Security.Claims;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace OpenPlanningPoker.GameEngine.GraphQL.Extensions;

/// <summary>
/// Browsers cannot send an Authorization header on a WebSocket, so the app sends the token as
/// <c>authToken</c> in the graphql-ws connection_init payload. The token is validated with the same
/// rules as HTTP requests and becomes the user of every subscription on the socket. A missing or
/// invalid token keeps the socket anonymous: subscriptions that need a player then refuse to start.
/// </summary>
public sealed class WebSocketAuthInterceptor : DefaultSocketSessionInterceptor
{
    public override async ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
    {
        var token = ReadToken(connectionInitMessage);
        if (token is not null)
        {
            var httpContext = session.Connection.HttpContext;
            if (await ValidateAsync(httpContext.RequestServices, token) is { } user)
            {
                httpContext.User = user;
            }
        }

        return await base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
    }

    private static string? ReadToken(IOperationMessagePayload payload)
    {
        try
        {
            var token = payload.As<ConnectionInitPayload>()?.AuthToken;
            if (string.IsNullOrWhiteSpace(token)) return null;
            return token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token["Bearer ".Length..] : token;
        }
        catch
        {
            return null; // Malformed payload: stay anonymous.
        }
    }

    private static Task<ClaimsPrincipal?> ValidateAsync(IServiceProvider services, string token) =>
        ValidateTokenAsync(
            services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme),
            token);

    /// <summary>Validates a token with the rules of the HTTP bearer scheme; null when it is not valid.</summary>
    public static async Task<ClaimsPrincipal?> ValidateTokenAsync(JwtBearerOptions options, string token)
    {
        foreach (var handler in options.TokenHandlers)
        {
            var result = await handler.ValidateTokenAsync(token, options.TokenValidationParameters);
            if (result.IsValid)
            {
                return new ClaimsPrincipal(result.ClaimsIdentity);
            }
        }

        return null;
    }

    private sealed record ConnectionInitPayload(string? AuthToken);
}
