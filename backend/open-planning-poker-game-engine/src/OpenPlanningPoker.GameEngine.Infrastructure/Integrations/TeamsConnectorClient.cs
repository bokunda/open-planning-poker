using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Chat;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Integrations;

/// <summary>
/// Sends messages to Teams through the Bot Connector REST API (OPP-23), with an app token from Microsoft
/// Entra (client credentials). No Bot Framework SDK: these two calls are all the app needs.
/// </summary>
public sealed class TeamsConnectorClient(HttpClient http, IOptions<ChatOptions> options) : ITeamsApi
{
    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static (string Token, DateTimeOffset ExpiresOn)? _token;
    private readonly ChatOptions _options = options.Value;

    public async Task SendAsync(string serviceUrl, string conversationId, object activity, CancellationToken cancellationToken)
    {
        var url = $"{serviceUrl.TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(cancellationToken));
        request.Content = JsonContent.Create(activity);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new ChatApiException($"Teams: {(int)response.StatusCode}");
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is { } cached && cached.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5)) return cached.Token;
        await TokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_token is { } again && again.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5)) return again.Token;
            var tenant = string.IsNullOrWhiteSpace(_options.TeamsTenantId) ? "botframework.com" : _options.TeamsTenantId;
            using var response = await http.PostAsync($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.TeamsAppId,
                    ["client_secret"] = _options.TeamsAppPassword,
                    ["scope"] = "https://api.botframework.com/.default",
                }), cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ChatApiException($"Teams token: {(int)response.StatusCode}");
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var token = doc.RootElement.GetProperty("access_token").GetString()!;
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
            _token = (token, DateTimeOffset.UtcNow.AddSeconds(expiresIn));
            return token;
        }
        finally
        {
            TokenLock.Release();
        }
    }
}
