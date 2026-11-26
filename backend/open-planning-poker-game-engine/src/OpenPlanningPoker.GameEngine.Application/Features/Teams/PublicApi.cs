using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;
using OpenPlanningPoker.GameEngine.Domain.Tickets.Events;

namespace OpenPlanningPoker.GameEngine.Application.Features.Teams;

// ---------------------------------------------------------------- what the public API returns (OPP-159)

/// <summary>A planning session (sprint) of the team, with its numbers. No votes per player and no player names.</summary>
public sealed record ApiSession(
    Guid Id, string Name, DateTimeOffset CreatedOn, int TicketCount, int EstimatedCount, double Points,
    double? FirstRoundConsensusShare, double? ReVotesPerTicket, double? AverageDiscussionSeconds, int NeedsInfo);

/// <summary>A ticket of a session: its link in the tracker and the agreed estimate.</summary>
public sealed record ApiTicket(
    Guid Id, Guid SessionId, string Name, string? ExternalKey, string? ExternalUrl, string? Provider, string? Estimate,
    bool Estimated, bool NeedsInfo, DateTimeOffset? EstimatedAt);

public sealed record ApiPage<T>(IReadOnlyList<T> Items, int Total, int Skip, int Take);

public sealed record ApiTeam(Guid Id, string Name, string Description);

// ---------------------------------------------------------------- what the team owner manages (OPP-158, OPP-160)

public sealed record ApiTokenInfo(Guid Id, string Name, string Prefix, DateTimeOffset CreatedOn, DateTimeOffset? LastUsedOn, bool Active);

/// <summary>A new token: <see cref="Token"/> is shown once and never again.</summary>
public sealed record NewApiToken(ApiTokenInfo Info, string Token);

public sealed record WebhookInfo(Guid Id, string Url, IReadOnlyList<string> Events, bool Enabled, DateTimeOffset CreatedOn);

/// <summary>A new or rotated webhook: <see cref="Secret"/> is shown once.</summary>
public sealed record NewWebhook(WebhookInfo Info, string Secret);

public sealed record WebhookDeliveryInfo(Guid Id, string Event, Guid EventId, int Attempt, int? StatusCode, bool Success, string? Error, double DurationMs, DateTimeOffset CreatedOn);

public static class PublicApiErrors
{
    public static ApplicationError InvalidName => new("InvalidTokenName", $"Give the token a name of 1 to {TeamApiToken.MaxNameLength} characters.");
    public static ApplicationError TooManyTokens => new("TooManyTokens", $"A team can have up to {TeamApiToken.MaxPerTeam} active tokens.");
    public static ApplicationError TokenNotFound => new("TokenNotFound", "Token not found.");
    public static ApplicationError InvalidWebhookUrl => new("InvalidWebhookUrl", "Use a public https address for the webhook.");
    public static ApplicationError InvalidEvents => new("InvalidWebhookEvents", "Choose ticket.estimated, session.finished or both.");
    public static ApplicationError TooManyWebhooks => new("TooManyWebhooks", $"A team can have up to {TeamWebhook.MaxPerTeam} webhooks.");
    public static ApplicationError WebhookNotFound => new("WebhookNotFound", "Webhook not found.");
}

/// <summary>An event waiting to be sent to one webhook.</summary>
public sealed record WebhookJob(Guid WebhookId, string Event, Guid EventId, string Body, int Attempt = 1);

/// <summary>The queue the webhook sender works through in the background, so requests never wait for other servers.</summary>
public interface IWebhookQueue
{
    void Enqueue(WebhookJob job);
}

/// <summary>Public API of a team (OPP-141): tokens, the read-only data and outgoing webhooks.</summary>
public sealed class TeamApiService(
    ITeamApiRepository api,
    ITeamRepository teams,
    IGameRepository games,
    ITicketRepository tickets,
    IVoteRepository votes,
    ISender sender,
    ISecretProtector protector,
    IUnitOfWork unitOfWork,
    IAuditLog auditLog)
{
    public const string TokenPrefix = "opp_";
    public const string SecretPrefix = "whsec_";
    public const int MaxTake = 100;

    // ------------------------------------------------------------ tokens

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string RandomPart() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(30)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public async Task<Result<NewApiToken, ApplicationError>> CreateTokenAsync(Guid teamId, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > TeamApiToken.MaxNameLength) return PublicApiErrors.InvalidName;
        if ((await api.GetTokensAsync(teamId, cancellationToken)).Count(t => t.IsActive) >= TeamApiToken.MaxPerTeam) return PublicApiErrors.TooManyTokens;

        var raw = TokenPrefix + RandomPart();
        var token = TeamApiToken.Create(teamId, name, Hash(raw), raw[..12]);
        api.AddToken(token);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Team(AuditActions.TeamApiTokenCreated, AuditType.Create, teamId, detail: token.Name), cancellationToken);
        return new NewApiToken(Info(token), raw);
    }

    public async Task<IReadOnlyList<ApiTokenInfo>> TokensAsync(Guid teamId, CancellationToken cancellationToken) =>
        [.. (await api.GetTokensAsync(teamId, cancellationToken)).OrderByDescending(t => t.CreatedOn).Select(Info)];

    public async Task<Result<bool, ApplicationError>> RevokeTokenAsync(Guid teamId, Guid tokenId, CancellationToken cancellationToken)
    {
        var token = await api.GetTokenAsync(tokenId, cancellationToken);
        if (token is null || token.TeamId != teamId) return PublicApiErrors.TokenNotFound;
        token.Revoke();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Team(AuditActions.TeamApiTokenRevoked, AuditType.Delete, teamId, detail: token.Name), cancellationToken);
        return true;
    }

    /// <summary>The team of an active token, or null. Records the last use.</summary>
    public async Task<TeamApiToken?> AuthenticateAsync(string? raw, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(raw) || !raw.StartsWith(TokenPrefix, StringComparison.Ordinal) || raw.Length > 100) return null;
        var token = await api.GetTokenByHashAsync(Hash(raw), cancellationToken);
        if (token is null || !token.IsActive) return null;
        if (token.Used(DateTimeOffset.UtcNow)) await unitOfWork.SaveChangesAsync(cancellationToken);
        return token;
    }

    private static ApiTokenInfo Info(TeamApiToken t) => new(t.Id, t.Name, t.Prefix, t.CreatedOn, t.LastUsedOn, t.IsActive);

    // ------------------------------------------------------------ read-only data (OPP-159)

    public async Task<ApiTeam?> TeamAsync(Guid teamId, CancellationToken cancellationToken) =>
        await teams.GetByIdAsync(teamId, cancellationToken) is { } team ? new ApiTeam(team.Id, team.Name, team.Description) : null;

    public async Task<ApiPage<ApiSession>> SessionsAsync(Guid teamId, int skip, int take, CancellationToken cancellationToken)
    {
        (skip, take) = Paging(skip, take);
        var sprints = await sender.Send(new GetTeamSprintsQuery(teamId), cancellationToken);
        var items = sprints.Skip(skip).Take(take).Select(s => new ApiSession(
            s.GameId, s.Name, s.CreatedOn, s.TicketCount, s.EstimatedCount, s.Points,
            s.Stats?.FirstRoundConsensusShare, s.Stats?.ReVotesPerTicket, s.Stats?.AverageDiscussionSeconds, s.Stats?.NeedsInfo ?? 0)).ToList();
        return new ApiPage<ApiSession>(items, sprints.Count, skip, take);
    }

    /// <summary>Tickets of a session of the team; null when the session is not the team's.</summary>
    public async Task<ApiPage<ApiTicket>?> TicketsAsync(Guid teamId, Guid sessionId, int skip, int take, CancellationToken cancellationToken)
    {
        (skip, take) = Paging(skip, take);
        var game = await games.GetByIdAsync(sessionId, cancellationToken);
        if (game is null || game.TeamId != teamId) return null;
        var all = (await tickets.GetByGame(sessionId, cancellationToken)).ToList();
        var gameVotes = await votes.GetByGame(sessionId, null, cancellationToken);
        var items = all.Skip(skip).Take(take).Select(t =>
        {
            var estimate = Teams.EstimateOf(t, [.. gameVotes.Where(v => v.TicketId == t.Id).Select(v => v.Value)]);
            return new ApiTicket(t.Id, sessionId, t.Name, t.ExternalKey, t.ExternalUrl, t.ExternalProvider ?? (t.ExternalKey is null ? null : "jira"),
                estimate, t.IsEstimated, t.NeedsInfo, t.EstimatedAt);
        }).ToList();
        return new ApiPage<ApiTicket>(items, all.Count, skip, take);
    }

    private static (int Skip, int Take) Paging(int skip, int take) => (Math.Max(0, skip), Math.Clamp(take <= 0 ? 50 : take, 1, MaxTake));

    // ------------------------------------------------------------ webhooks (OPP-160)

    /// <summary>
    /// Only https to a host name or a public address. The sender checks again at connect time, so a name that later
    /// points to an internal address is refused too.
    /// </summary>
    public static bool IsAllowedUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > TeamWebhook.MaxUrlLength) return false;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return false;
        if (!string.IsNullOrEmpty(uri.UserInfo)) return false;
        var host = uri.IdnHost.ToLowerInvariant();
        if (host is "localhost" || host.EndsWith(".localhost") || host.EndsWith(".local") || host.EndsWith(".internal")) return false;
        return !IPAddress.TryParse(host.Trim('[', ']'), out var ip) || IsPublic(ip);
    }

    /// <summary>False for loopback, private, link-local, carrier-grade NAT, multicast and other reserved ranges.</summary>
    public static bool IsPublic(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.Broadcast)) return false;
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return !(ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast || ip.IsIPv6UniqueLocal);
        }

        var b = ip.GetAddressBytes();
        return !(b[0] == 0 || b[0] == 10 || b[0] == 127 || b[0] >= 224
            || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
            || (b[0] == 169 && b[1] == 254)
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 192 && b[1] == 0 && b[2] == 0)
            || (b[0] == 198 && (b[1] == 18 || b[1] == 19)));
    }

    private static bool ValidEvents(IReadOnlyCollection<string> events) => events.Count > 0 && events.All(TeamWebhook.Events.Contains);

    public async Task<Result<NewWebhook, ApplicationError>> CreateWebhookAsync(Guid teamId, string url, IReadOnlyCollection<string> events, CancellationToken cancellationToken)
    {
        if (!IsAllowedUrl(url)) return PublicApiErrors.InvalidWebhookUrl;
        if (!ValidEvents(events)) return PublicApiErrors.InvalidEvents;
        if ((await api.GetWebhooksAsync(teamId, cancellationToken)).Count >= TeamWebhook.MaxPerTeam) return PublicApiErrors.TooManyWebhooks;

        var secret = SecretPrefix + RandomPart();
        var webhook = TeamWebhook.Create(teamId, url, protector.Protect(secret), events);
        api.AddWebhook(webhook);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Team(AuditActions.TeamWebhookCreated, AuditType.Create, teamId, detail: WebhookHost(webhook.Url)), cancellationToken);
        return new NewWebhook(Info(webhook), secret);
    }

    public async Task<Result<WebhookInfo, ApplicationError>> UpdateWebhookAsync(Guid teamId, Guid webhookId, string url, IReadOnlyCollection<string> events, bool enabled, CancellationToken cancellationToken)
    {
        var webhook = await api.GetWebhookAsync(webhookId, cancellationToken);
        if (webhook is null || webhook.TeamId != teamId) return PublicApiErrors.WebhookNotFound;
        if (!IsAllowedUrl(url)) return PublicApiErrors.InvalidWebhookUrl;
        if (!ValidEvents(events)) return PublicApiErrors.InvalidEvents;
        webhook.Update(url, events, enabled);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Team(AuditActions.TeamWebhookUpdated, AuditType.Update, teamId, detail: WebhookHost(webhook.Url)), cancellationToken);
        return Info(webhook);
    }

    public async Task<Result<NewWebhook, ApplicationError>> RotateSecretAsync(Guid teamId, Guid webhookId, CancellationToken cancellationToken)
    {
        var webhook = await api.GetWebhookAsync(webhookId, cancellationToken);
        if (webhook is null || webhook.TeamId != teamId) return PublicApiErrors.WebhookNotFound;
        var secret = SecretPrefix + RandomPart();
        webhook.SetSecret(protector.Protect(secret));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Team(AuditActions.TeamWebhookSecretRotated, AuditType.Update, teamId, detail: WebhookHost(webhook.Url)), cancellationToken);
        return new NewWebhook(Info(webhook), secret);
    }

    public async Task<Result<bool, ApplicationError>> DeleteWebhookAsync(Guid teamId, Guid webhookId, CancellationToken cancellationToken)
    {
        var webhook = await api.GetWebhookAsync(webhookId, cancellationToken);
        if (webhook is null || webhook.TeamId != teamId) return PublicApiErrors.WebhookNotFound;
        api.RemoveDeliveries(await api.GetDeliveriesAsync(webhookId, cancellationToken));
        api.RemoveWebhook(webhook);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(AuditEntry.Team(AuditActions.TeamWebhookDeleted, AuditType.Delete, teamId, detail: WebhookHost(webhook.Url)), cancellationToken);
        return true;
    }

    /// <summary>Only the host goes to the activity log: webhook paths often carry a secret (e.g. Zapier, Slack).</summary>
    public static string WebhookHost(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : string.Empty;

    public async Task<IReadOnlyList<WebhookInfo>> WebhooksAsync(Guid teamId, CancellationToken cancellationToken) =>
        [.. (await api.GetWebhooksAsync(teamId, cancellationToken)).Select(Info)];

    public async Task<Result<IReadOnlyList<WebhookDeliveryInfo>, ApplicationError>> DeliveriesAsync(Guid teamId, Guid webhookId, CancellationToken cancellationToken)
    {
        var webhook = await api.GetWebhookAsync(webhookId, cancellationToken);
        if (webhook is null || webhook.TeamId != teamId) return PublicApiErrors.WebhookNotFound;
        return (await api.GetDeliveriesAsync(webhookId, cancellationToken))
            .OrderByDescending(d => d.CreatedOn)
            .Select(d => new WebhookDeliveryInfo(d.Id, d.Event, d.EventId, d.Attempt, d.StatusCode, d.Success, d.Error, d.DurationMs, d.CreatedOn))
            .ToList();
    }

    private static WebhookInfo Info(TeamWebhook w) => new(w.Id, w.Url, w.SubscribedEvents, w.Enabled, w.CreatedOn);
}

/// <summary>The body and the signature of webhook requests (OPP-160).</summary>
public static class WebhookSigning
{
    public const string SignatureHeader = "X-OPP-Signature";
    public const string EventHeader = "X-OPP-Event";
    public const string DeliveryHeader = "X-OPP-Delivery";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary><c>t=&lt;unix seconds&gt;,v1=&lt;hex HMAC-SHA256 of "t.body"&gt;</c>, like Stripe, so a receiver can also reject old requests.</summary>
    public static string Sign(string secret, long timestamp, string body)
    {
        var mac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}"));
        return $"t={timestamp},v1={Convert.ToHexString(mac).ToLowerInvariant()}";
    }

    public static bool Verify(string secret, string header, string body, DateTimeOffset now, TimeSpan tolerance)
    {
        var parts = header.Split(',').Select(p => p.Split('=', 2)).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
        if (!parts.TryGetValue("t", out var t) || !long.TryParse(t, out var ts) || !parts.TryGetValue("v1", out var v1)) return false;
        if (Math.Abs(now.ToUnixTimeSeconds() - ts) > tolerance.TotalSeconds) return false;
        var expected = Sign(secret, ts, body).Split("v1=")[1];
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(v1));
    }

    public static string Body(string eventName, Guid eventId, Guid teamId, object data) =>
        JsonSerializer.Serialize(new { id = eventId, type = eventName, createdAt = DateTimeOffset.UtcNow, teamId, data }, Json);
}

/// <summary>
/// Turns a final estimate into webhook events of the sprint's team: <c>ticket.estimated</c> every time, and
/// <c>session.finished</c> when the last open ticket of the session got its estimate.
/// </summary>
public sealed class TicketEstimatedWebhooks(
    IGameRepository games, ITicketRepository tickets, ITeamApiRepository api, IWebhookQueue queue)
    : INotificationHandler<TicketEstimatedDomainEvent>
{
    public async Task Handle(TicketEstimatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var game = await games.GetByIdAsync(notification.GameId, cancellationToken);
        if (game?.TeamId is not { } teamId) return;
        var hooks = (await api.GetWebhooksAsync(teamId, cancellationToken)).Where(w => w.Enabled).ToList();
        if (hooks.Count == 0) return;

        var all = (await tickets.GetByGame(game.Id, cancellationToken)).ToList();
        var ticket = all.FirstOrDefault(t => t.Id == notification.TicketId);
        if (ticket is null) return;

        var estimated = new
        {
            sessionId = game.Id, sessionName = game.Name,
            ticket = new { id = ticket.Id, name = ticket.Name, externalKey = ticket.ExternalKey, externalUrl = ticket.ExternalUrl, estimate = ticket.FinalEstimate },
            changed = !notification.WasOpen,
        };
        Send(hooks, "ticket.estimated", teamId, estimated);

        if (notification.WasOpen && all.All(t => t.FinalEstimate is not null))
        {
            var points = all.Sum(t => Planning.PointsOf(t.FinalEstimate) ?? 0);
            Send(hooks, "session.finished", teamId, new { sessionId = game.Id, sessionName = game.Name, ticketCount = all.Count, points });
        }
    }

    private void Send(IEnumerable<TeamWebhook> hooks, string eventName, Guid teamId, object data)
    {
        var eventId = Guid.NewGuid();
        var body = WebhookSigning.Body(eventName, eventId, teamId, data);
        foreach (var hook in hooks.Where(h => h.SubscribedEvents.Contains(eventName)))
        {
            queue.Enqueue(new WebhookJob(hook.Id, eventName, eventId, body));
        }
    }
}
