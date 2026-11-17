using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using OpenPlanningPoker.GameEngine.Application.Features.GameSettings;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;
using OpenPlanningPoker.GameEngine.Domain.Integrations;

namespace OpenPlanningPoker.GameEngine.Application.Features.Integrations.Chat;

/// <summary>Slack (OPP-22) and Microsoft Teams (OPP-23) apps. Each is off until its credentials are set.</summary>
public sealed class ChatOptions
{
    public const string Section = "Chat";

    /// <summary>Where players open games.</summary>
    public string AppUrl { get; set; } = "https://app.openplanningpoker.com";

    /// <summary>Public URL of this service (Slack OAuth redirect, Teams endpoints).</summary>
    public string ApiUrl { get; set; } = "https://ge.openplanningpoker.com";

    public string SlackClientId { get; set; } = string.Empty;
    public string SlackClientSecret { get; set; } = string.Empty;
    public string SlackSigningSecret { get; set; } = string.Empty;

    public string TeamsAppId { get; set; } = string.Empty;
    public string TeamsAppPassword { get; set; } = string.Empty;

    /// <summary>Single-tenant bots: the Microsoft Entra tenant of the bot registration. Empty for multi-tenant.</summary>
    public string TeamsTenantId { get; set; } = string.Empty;

    public bool SlackEnabled => !string.IsNullOrWhiteSpace(SlackClientId) && !string.IsNullOrWhiteSpace(SlackClientSecret) && !string.IsNullOrWhiteSpace(SlackSigningSecret);
    public bool TeamsEnabled => !string.IsNullOrWhiteSpace(TeamsAppId) && !string.IsNullOrWhiteSpace(TeamsAppPassword);
    public string SlackRedirectUri => $"{ApiUrl.TrimEnd('/')}/integrations/slack/oauth";
}

public sealed record SlackOAuthResult(string WorkspaceId, string WorkspaceName, string BotToken);

public sealed class ChatApiException(string message) : Exception(message);

public interface ISlackApi
{
    Task<SlackOAuthResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>Posts to a channel (in a thread when threadTs is set) and returns the message ts.</summary>
    Task<string> PostMessageAsync(string botToken, string channelId, string text, object[]? blocks, string? threadTs, CancellationToken cancellationToken);
}

public interface ITeamsApi
{
    /// <summary>Sends an activity (a message) to a Teams conversation through the Bot Connector.</summary>
    Task SendAsync(string serviceUrl, string conversationId, object activity, CancellationToken cancellationToken);
}

/// <summary>Queue for posting reveal results to chat without making the reveal wait for Slack or Teams.</summary>
public interface IChatNotifications
{
    void TicketRevealed(Guid ticketId);
}

public sealed record StartedGame(Guid GameId, string JoinUrl, string HostUrl);

/// <summary>Games started from Slack or Teams: creation with a host claim link, the claim, and the results.</summary>
public sealed class ChatGameService(
    IOptions<ChatOptions> options,
    IGameRepository games,
    IGameSettingsRepository settings,
    ITicketRepository tickets,
    IVoteRepository votes,
    IChatIntegrationRepository chat,
    ISlackApi slack,
    ITeamsApi teams,
    ISecretProtector protector,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan ClaimLifetime = TimeSpan.FromDays(7);
    private readonly ChatOptions _options = options.Value;

    public ChatOptions Options => _options;

    /// <summary>
    /// Creates a game without a host and returns its join link and the one-time host link. Only the hash of the
    /// host link's token is stored.
    /// </summary>
    public async Task<StartedGame> StartGameAsync(string name, CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var title = string.IsNullOrWhiteSpace(name) ? "Planning poker" : name.Trim();
        if (title.Length > 100) title = title[..100];

        var game = Game.CreateUnclaimed(title, string.Empty, Hash(token), DateTimeOffset.UtcNow.Add(ClaimLifetime));
        games.Add(game);
        settings.Add(Domain.GameSettings.GameSettings.Create(game.Id, GameSettingsConstants.FibonacciDeckSetup));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var joinUrl = $"{_options.AppUrl.TrimEnd('/')}/game/{game.Id}";
        return new StartedGame(game.Id, joinUrl, $"{joinUrl}?claim={token}");
    }

    /// <summary>The first player who opens the host link becomes the host.</summary>
    public async Task<bool> ClaimHostAsync(Guid gameId, string token, Guid playerId, CancellationToken cancellationToken)
    {
        var game = await games.GetByIdAsync(gameId, cancellationToken);
        if (game is null || !game.TryClaimHost(Hash(token), playerId, DateTimeOffset.UtcNow)) return false;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RecordPostAsync(ChatPost post, CancellationToken cancellationToken)
    {
        chat.AddPost(post);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveSlackInstallationAsync(SlackOAuthResult result, CancellationToken cancellationToken)
    {
        var installation = await chat.GetSlackInstallationAsync(result.WorkspaceId, cancellationToken);
        if (installation is null)
        {
            installation = SlackInstallation.Create(result.WorkspaceId);
            chat.AddSlackInstallation(installation);
        }
        installation.Update(result.WorkspaceName, protector.Protect(result.BotToken));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The workspace's bot token, or null when the app is not installed there.</summary>
    public async Task<string?> GetSlackBotTokenAsync(string workspaceId, CancellationToken cancellationToken)
    {
        var installation = await chat.GetSlackInstallationAsync(workspaceId, cancellationToken);
        return installation is null ? null : protector.Unprotect(installation.ProtectedBotToken);
    }

    /// <summary>Posts "ticket — estimate (votes)" to the chat the game was started from. Does nothing for other games.</summary>
    public async Task PostResultAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket is null || !ticket.VotesRevealed) return;
        var post = await chat.GetPostAsync(ticket.GameId, cancellationToken);
        if (post is null) return;

        var values = (await votes.GetByTicket(ticketId, null, cancellationToken)).Select(vote => vote.Value).ToList();
        var text = ResultText(ticket.Name, values);

        if (post.Provider == ChatProviders.Slack && _options.SlackEnabled)
        {
            var token = await GetSlackBotTokenAsync(post.WorkspaceId, cancellationToken);
            if (token is not null) await slack.PostMessageAsync(token, post.ChannelId, text, null, post.MessageId, cancellationToken);
        }
        else if (post.Provider == ChatProviders.Teams && _options.TeamsEnabled && post.ServiceUrl is not null)
        {
            await teams.SendAsync(post.ServiceUrl, post.ChannelId, new { type = "message", text, textFormat = "markdown" }, cancellationToken);
        }
    }

    public static string ResultText(string ticketName, IReadOnlyCollection<string> values)
    {
        var stats = TicketStats.From(true, values);
        if (values.Count == 0) return $"*{ticketName}*: nobody voted.";
        var distribution = string.Join(", ", stats.Distribution.Select(count => count.Count == 1 ? count.Value : $"{count.Value} ×{count.Count}"));
        var spread = stats.Spread switch
        {
            VoteSpread.Consensus => "consensus",
            VoteSpread.Close => "close to consensus",
            _ => "wide spread",
        };
        return $"*{ticketName}*: estimate *{stats.Estimate}* ({spread}). Votes: {distribution}";
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
