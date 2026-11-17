using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Chat;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Integrations;

/// <summary>Slack Web API: OAuth v2 install and chat.postMessage (OPP-22).</summary>
public sealed class SlackApiClient(HttpClient http, IOptions<ChatOptions> options) : ISlackApi
{
    private readonly ChatOptions _options = options.Value;

    public async Task<SlackOAuthResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsync("https://slack.com/api/oauth.v2.access", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.SlackClientId,
            ["client_secret"] = _options.SlackClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _options.SlackRedirectUri,
        }), cancellationToken);
        using var doc = await ReadAsync(response, cancellationToken);
        var root = doc.RootElement;
        var team = root.GetProperty("team");
        var name = team.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
        return new SlackOAuthResult(team.GetProperty("id").GetString()!, name, root.GetProperty("access_token").GetString()!);
    }

    public async Task<string> PostMessageAsync(string botToken, string channelId, string text, object[]? blocks, string? threadTs, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
        var body = new Dictionary<string, object> { ["channel"] = channelId, ["text"] = text, ["unfurl_links"] = false };
        if (blocks is not null) body["blocks"] = blocks;
        if (threadTs is not null) body["thread_ts"] = threadTs;
        request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request, cancellationToken);
        using var doc = await ReadAsync(response, cancellationToken);
        return doc.RootElement.GetProperty("ts").GetString()!;
    }

    /// <summary>Slack answers 200 with ok=false on errors; both become a <see cref="ChatApiException"/>.</summary>
    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!response.IsSuccessStatusCode || !doc.RootElement.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
        {
            var error = doc.RootElement.TryGetProperty("error", out var e) ? e.GetString() : response.StatusCode.ToString();
            doc.Dispose();
            throw new ChatApiException($"Slack: {error}");
        }
        return doc;
    }
}
