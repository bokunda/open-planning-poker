using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Chat;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;
using OpenPlanningPoker.GameEngine.Domain.Integrations;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Integrations;

/// <summary>Reveal results wait here, so a reveal never waits for Slack or Teams.</summary>
public sealed class ChatNotificationQueue : IChatNotifications
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest });
    public ChannelReader<Guid> Reader => _channel.Reader;
    public void TicketRevealed(Guid ticketId) => _channel.Writer.TryWrite(ticketId);
}

/// <summary>Posts each queued reveal to the chat the game was started from (OPP-22, OPP-23).</summary>
public sealed class ChatNotificationWorker(ChatNotificationQueue queue, IServiceScopeFactory scopes, ILogger<ChatNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var ticketId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ChatGameService>().PostResultAsync(ticketId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not post the result of ticket {TicketId} to chat", ticketId);
            }
        }
    }
}

[MutationType]
public class ChatMutations
{
    /// <summary>
    /// Makes the caller the host of a game started from Slack or Teams, with the one-time link from the chat.
    /// False when the game already has a host or the link is not valid.
    /// </summary>
    public async Task<FieldResult<bool, ApplicationError>> ClaimGameHostAsync(
        [Service] ChatGameService chat,
        [Service] IGameAccessService gameAccess,
        [Service] ITopicEventSender eventSender,
        [Required] Guid gameId,
        [Required] string token,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var claimed = await chat.ClaimHostAsync(gameId, token, gameAccess.CurrentUserId, cancellationToken);
        if (claimed) await eventSender.PublishAsync(gameId, GameChangeKind.Game, cancellationToken: cancellationToken);
        return claimed;
    }
}

public static class ChatIntegrationEndpoints
{
    public static IServiceCollection AddChatIntegrations(this IServiceCollection services)
    {
        services.AddSingleton<ChatNotificationQueue>();
        services.AddSingleton<IChatNotifications>(sp => sp.GetRequiredService<ChatNotificationQueue>());
        services.AddHostedService<ChatNotificationWorker>();
        services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(_ =>
            new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://login.botframework.com/v1/.well-known/openidconfiguration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = true }));
        return services;
    }

    public static void MapChatIntegrations(this WebApplication app)
    {
        var slack = app.MapGroup("/integrations/slack");
        slack.MapGet("/install", SlackInstall);
        slack.MapGet("/oauth", SlackOAuthAsync);
        slack.MapPost("/commands", SlackCommandAsync);

        var teams = app.MapGroup("/integrations/teams");
        teams.MapPost("/messages", TeamsMessageAsync);
        teams.MapGet("/tab-config", TeamsTabConfig);
    }

    // ---- Slack (OPP-22) ----

    private static IResult SlackInstall(IOptions<ChatOptions> options, ISecretProtector protector)
    {
        var o = options.Value;
        if (!o.SlackEnabled) return Results.NotFound();
        var state = protector.ProtectState("slack-install", TimeSpan.FromMinutes(15));
        var url = "https://slack.com/oauth/v2/authorize"
            + $"?client_id={Uri.EscapeDataString(o.SlackClientId)}"
            + "&scope=" + Uri.EscapeDataString("commands,chat:write,chat:write.public")
            + $"&redirect_uri={Uri.EscapeDataString(o.SlackRedirectUri)}"
            + $"&state={Uri.EscapeDataString(state)}";
        return Results.Redirect(url);
    }

    private static async Task<IResult> SlackOAuthAsync(
        string? code, string? state, string? error,
        IOptions<ChatOptions> options, ISecretProtector protector, ISlackApi slack, ChatGameService chat, CancellationToken cancellationToken)
    {
        if (!options.Value.SlackEnabled) return Results.NotFound();
        if (error is not null || code is null || state is null || protector.UnprotectState(state) != "slack-install")
        {
            return Page("Slack was not connected", "The installation was cancelled or the link expired. Start again from the install link.");
        }
        try
        {
            var result = await slack.ExchangeCodeAsync(code, cancellationToken);
            await chat.SaveSlackInstallationAsync(result, cancellationToken);
            return Page("Open Planning Poker is in Slack",
                $"It is installed in {System.Net.WebUtility.HtmlEncode(result.WorkspaceName)}. In any channel, type <code>/poker Sprint 43</code> to start a game.");
        }
        catch (ChatApiException)
        {
            return Page("Slack was not connected", "Slack did not accept the installation. Try again.");
        }
    }

    private static async Task<IResult> SlackCommandAsync(
        HttpRequest request, IOptions<ChatOptions> options, ChatGameService chat, ISlackApi slack, CancellationToken cancellationToken)
    {
        var o = options.Value;
        if (!o.SlackEnabled) return Results.NotFound();

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (!SlackSignature.IsValid(o.SlackSigningSecret, request.Headers["X-Slack-Request-Timestamp"], request.Headers["X-Slack-Signature"], body, DateTimeOffset.UtcNow))
        {
            return Results.Unauthorized();
        }

        var form = System.Web.HttpUtility.ParseQueryString(body);
        var workspaceId = form["team_id"] ?? string.Empty;
        var channelId = form["channel_id"] ?? string.Empty;
        var userId = form["user_id"] ?? string.Empty;
        var name = (form["text"] ?? string.Empty).Trim();

        var botToken = await chat.GetSlackBotTokenAsync(workspaceId, cancellationToken);
        if (botToken is null)
        {
            return Ephemeral($"Open Planning Poker is not installed in this workspace yet: {o.ApiUrl.TrimEnd('/')}/integrations/slack/install");
        }

        var game = await chat.StartGameAsync(name, cancellationToken);
        var title = string.IsNullOrWhiteSpace(name) ? "Planning poker" : name;
        var text = $"<@{userId}> started planning poker: *{Escape(title)}*";
        object[] blocks =
        [
            new { type = "section", text = new { type = "mrkdwn", text } },
            new
            {
                type = "actions",
                elements = new object[] { new { type = "button", text = new { type = "plain_text", text = "Join" }, url = game.JoinUrl, style = "primary", action_id = "join" } }
            },
        ];

        var shared = true;
        try
        {
            var ts = await slack.PostMessageAsync(botToken, channelId, text, blocks, null, cancellationToken);
            await chat.RecordPostAsync(ChatPost.Create(game.GameId, ChatProviders.Slack, workspaceId, channelId, ts, null), cancellationToken);
        }
        catch (ChatApiException)
        {
            shared = false; // e.g. a private channel the app was not added to
        }

        var hostLink = $"<{game.HostUrl}|Open as host>";
        return Ephemeral(shared
            ? $"Your game is ready. {hostLink} (only you see this link; the first person to open it becomes the host). Results are posted in the thread after each reveal."
            : $"Your game is ready, but I could not post in this channel (add the app to it with /invite). Share this link: {game.JoinUrl}  {hostLink} (only you see this).");
    }

    private static IResult Ephemeral(string text) => Results.Json(new { response_type = "ephemeral", text });

    private static string Escape(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // ---- Microsoft Teams (OPP-23) ----

    private static async Task<IResult> TeamsMessageAsync(
        HttpRequest request, IOptions<ChatOptions> options, IConfigurationManager<OpenIdConnectConfiguration> botKeys,
        ChatGameService chat, ITeamsApi teams, CancellationToken cancellationToken)
    {
        var o = options.Value;
        if (!o.TeamsEnabled) return Results.NotFound();

        using var doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
        var activity = doc.RootElement;
        var serviceUrl = Str(activity, "serviceUrl");
        if (!await TeamsAuth.IsValidAsync(request.Headers.Authorization, o.TeamsAppId, serviceUrl, botKeys, cancellationToken))
        {
            return Results.Unauthorized();
        }

        var type = Str(activity, "type");
        var conversation = activity.TryGetProperty("conversation", out var c) ? c : default;
        var conversationId = conversation.ValueKind == JsonValueKind.Object ? Str(conversation, "id") : string.Empty;
        var tenantId = conversation.ValueKind == JsonValueKind.Object ? Str(conversation, "tenantId") : string.Empty;

        if (type == "invoke" && Str(activity, "name") == "composeExtension/submitAction")
        {
            var value = activity.GetProperty("value");
            var name = value.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object ? Str(data, "name") : string.Empty;
            var game = await chat.StartGameAsync(name, cancellationToken);
            if (conversationId.Length > 0 && serviceUrl.Length > 0)
            {
                await chat.RecordPostAsync(ChatPost.Create(game.GameId, ChatProviders.Teams, tenantId, conversationId, null, serviceUrl), cancellationToken);
            }
            return Results.Json(new { composeExtension = new { type = "result", attachmentLayout = "list", attachments = new[] { TeamsCard(string.IsNullOrWhiteSpace(name) ? "Planning poker" : name.Trim(), game) } } });
        }

        if (type == "message" && conversationId.Length > 0)
        {
            await teams.SendAsync(serviceUrl, conversationId, new
            {
                type = "message",
                text = "Start a game from the **Open Planning Poker** message extension (the ⋯ under the message box), or add the app as a meeting tab.",
                textFormat = "markdown",
            }, cancellationToken);
        }
        return Results.Ok();
    }

    /// <summary>
    /// The card shared in the chat. Teams cannot show a link to one person only here, so the first person who
    /// opens it becomes the host; the host can hand the game over later.
    /// </summary>
    private static object TeamsCard(string title, StartedGame game) => new
    {
        contentType = "application/vnd.microsoft.card.adaptive",
        content = new Dictionary<string, object>
        {
            ["type"] = "AdaptiveCard",
            ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
            ["version"] = "1.4",
            ["body"] = new object[]
            {
                new { type = "TextBlock", text = "Planning poker", weight = "Bolder", size = "Medium" },
                new { type = "TextBlock", text = title, wrap = true },
                new { type = "TextBlock", text = "The first person to open the game runs it. Results are posted here after each reveal.", wrap = true, isSubtle = true, size = "Small" },
            },
            ["actions"] = new object[] { new { type = "Action.OpenUrl", title = "Join", url = game.HostUrl } },
        },
        preview = new
        {
            contentType = "application/vnd.microsoft.card.thumbnail",
            content = new { title, text = "Open Planning Poker" },
        },
    };

    /// <summary>Configuration page of the meeting tab: pick a game link, or open a new game.</summary>
    private static IResult TeamsTabConfig(IOptions<ChatOptions> options)
    {
        var appUrl = options.Value.AppUrl.TrimEnd('/');
        var html = $$"""
            <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Open Planning Poker tab</title>
            <script src="https://res.cdn.office.net/teams-js/2.22.0/js/MicrosoftTeams.min.js" crossorigin="anonymous"></script>
            <style>body{font-family:system-ui,sans-serif;margin:24px;color:#242424}input{width:100%;padding:8px;margin:8px 0;box-sizing:border-box}</style>
            </head><body>
            <h1 style="font-size:20px">Open Planning Poker</h1>
            <p>Paste the link of a game to show it in this meeting, or leave it empty to start a new game in the tab.</p>
            <input id="link" placeholder="{{appUrl}}/game/…" aria-label="Game link">
            <script>
              const app = '{{appUrl}}';
              microsoftTeams.app.initialize().then(() => {
                const input = document.getElementById('link');
                const update = () => {
                  const value = input.value.trim();
                  const ok = value === '' || value.startsWith(app + '/game/');
                  microsoftTeams.pages.config.setValidityState(ok);
                };
                input.addEventListener('input', update);
                microsoftTeams.pages.config.registerOnSaveHandler((save) => {
                  const url = input.value.trim() || (app + '/game');
                  microsoftTeams.pages.config.setConfig({ entityId: url, contentUrl: url, websiteUrl: url, suggestedDisplayName: 'Planning poker' })
                    .then(() => save.notifySuccess());
                });
                update();
              });
            </script></body></html>
            """;
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static IResult Page(string title, string message) => Results.Content(
        $"<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>{title}</title>"
        + "<style>body{font-family:system-ui,sans-serif;display:grid;place-items:center;min-height:90vh;margin:0;color:#18152b}main{max-width:460px;padding:24px;text-align:center}</style></head>"
        + $"<body><main><h1>{title}</h1><p>{message}</p><p><a href=\"https://openplanningpoker.com/\">Open Planning Poker</a></p></main></body></html>",
        "text/html; charset=utf-8");

    private static string Str(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}

/// <summary>Slack request signing: v0 HMAC-SHA256 of "v0:timestamp:body", at most 5 minutes old.</summary>
public static class SlackSignature
{
    public static bool IsValid(string signingSecret, string? timestamp, string? signature, string body, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature) || !long.TryParse(timestamp, out var seconds)) return false;
        if (Math.Abs(now.ToUnixTimeSeconds() - seconds) > 300) return false;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        var expected = "v0=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"v0:{timestamp}:{body}"))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature));
    }
}

/// <summary>Checks the Bot Framework token on requests from Teams: issuer, audience (our app id), keys and service URL.</summary>
public static class TeamsAuth
{
    public static async Task<bool> IsValidAsync(string? authorization, string appId, string serviceUrl,
        IConfigurationManager<OpenIdConnectConfiguration> keys, CancellationToken cancellationToken)
    {
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return false;
        var token = authorization["Bearer ".Length..].Trim();
        try
        {
            var config = await keys.GetConfigurationAsync(cancellationToken);
            var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
            var result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidIssuer = "https://api.botframework.com",
                ValidAudience = appId,
                IssuerSigningKeys = config.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
            });
            if (!result.IsValid) return false;
            var claimedServiceUrl = result.Claims.TryGetValue("serviceurl", out var value) ? value?.ToString() : null;
            return claimedServiceUrl is null || string.Equals(claimedServiceUrl.TrimEnd('/'), serviceUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is SecurityTokenException or InvalidOperationException or IOException or HttpRequestException)
        {
            return false;
        }
    }
}
