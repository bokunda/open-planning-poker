using System.Globalization;
using System.Text.Json;
using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;
using OpenPlanningPoker.GameEngine.Domain.Integrations;

namespace OpenPlanningPoker.GameEngine.Application.Features.Integrations.Trackers;

/// <summary>OAuth app of one tracker. Without a client id and secret the tracker is off and the app hides it.</summary>
public class TrackerOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

public sealed class GitHubOptions : TrackerOptions
{
    public const string Section = "GitHub";
    public GitHubOptions() => RedirectUri = "https://app.openplanningpoker.com/integrations/tracker/github/callback";
}

public sealed class LinearOptions : TrackerOptions
{
    public const string Section = "Linear";
    public LinearOptions() => RedirectUri = "https://app.openplanningpoker.com/integrations/tracker/linear/callback";
}

public sealed class AzureDevOpsOptions : TrackerOptions
{
    public const string Section = "AzureDevOps";
    public AzureDevOpsOptions() => RedirectUri = "https://app.openplanningpoker.com/integrations/tracker/azure-devops/callback";
}

public sealed record TrackerTokens(string AccessToken, string? RefreshToken, DateTimeOffset? ExpiresOn, string? AccountName);

/// <summary>Where issues come from: a repository, a team or a project. Id is the tracker's own reference.</summary>
public sealed record TrackerSource(string Id, string Name);

/// <summary>One issue for the import preview. Key is shown and stored on the ticket; Estimate is the current one.</summary>
public sealed record TrackerIssue(string Key, string Title, string Description, string Url, double? Estimate);

public sealed class TrackerApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
    public bool Unauthorized => Status == 401;
}

/// <summary>
/// One issue tracker behind the common import / save-back flow (OPP-130). Implementations call the tracker's API with
/// the player's access token; the service handles tokens, state, tickets and errors.
/// </summary>
public interface ITrackerClient
{
    /// <summary>"github", "linear" or "azure-devops".</summary>
    string Provider { get; }
    string DisplayName { get; }
    bool Enabled { get; }

    /// <summary>What the optional query filters, shown as the field's hint (labels, cycle, WIQL…).</summary>
    string QueryHint { get; }

    string AuthorizeUrl(string state);
    Task<TrackerTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>Null when the tracker has no refresh tokens.</summary>
    Task<TrackerTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task<IReadOnlyList<TrackerSource>> GetSourcesAsync(string accessToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<TrackerIssue>> GetIssuesAsync(string accessToken, string sourceId, string? query, int max, CancellationToken cancellationToken);

    /// <summary>Null when the value can be saved, else the reason (for example "only whole numbers").</summary>
    string? CheckEstimate(string value);

    Task SaveEstimateAsync(string accessToken, string issueKey, string issueUrl, string value, CancellationToken cancellationToken);
}

public sealed record TrackerStatus(string Provider, string Name, bool Enabled, bool Connected, bool NeedsReconnect, string? AccountName, string QueryHint);
public sealed record TrackerPreviewIssue(TrackerIssue Issue, bool AlreadyInGame);
public sealed record TrackerImportResult(int Imported, int Skipped);
public sealed record TrackerSaveResult(string IssueKey, string Value, string Url);

public static class TrackerErrors
{
    public static ApplicationError UnknownTracker => new("TrackerUnknown", "Unknown issue tracker.");
    public static ApplicationError NotConfigured => new("TrackerNotConfigured", "This tracker is not set up on this server.");
    public static ApplicationError NotConnected => new("TrackerNotConnected", "Connect the tracker first.");
    public static ApplicationError Reconnect => new("TrackerReconnect", "Access to the tracker was revoked or expired. Connect it again.");
    public static ApplicationError InvalidState => new("TrackerInvalidState", "The sign-in expired or does not belong to you. Try again.");
    public static ApplicationError Failed => new("TrackerFailed", "The tracker did not answer as expected. Try again in a moment.");
    public static ApplicationError NotLinked => new("TrackerNotLinked", "This ticket was not imported from this tracker.");
    public static ApplicationError NoEstimate => new("TrackerNoEstimate", "Reveal the cards and choose an estimate first.");
    public static ApplicationError Rejected(string reason) => new("TrackerEstimateRejected", reason);
}

/// <summary>
/// Import from and save to GitHub, Linear and Azure DevOps (OPP-131 to OPP-133) with one flow: connect once with
/// OAuth, pick a source, preview, import, then "Save to KEY" after the reveal. Jira keeps its own service (sites,
/// boards, story point fields) and the app shows it next to these.
/// </summary>
public sealed class TrackerService(
    IEnumerable<ITrackerClient> clients,
    ISecretProtector protector,
    ITrackerConnectionRepository connections,
    ITicketRepository tickets,
    IVoteRepository votes,
    IUnitOfWork unitOfWork)
{
    public const int MaxImport = 100;
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(15);
    private readonly Dictionary<string, ITrackerClient> _clients = clients.ToDictionary(c => c.Provider, StringComparer.OrdinalIgnoreCase);

    private sealed record OAuthState(Guid PlayerId, string Provider, string ReturnPath);

    public async Task<IReadOnlyList<TrackerStatus>> GetStatusAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var result = new List<TrackerStatus>();
        foreach (var client in _clients.Values)
        {
            var connection = client.Enabled ? await connections.GetAsync(playerId, client.Provider, cancellationToken) : null;
            result.Add(new TrackerStatus(client.Provider, client.DisplayName, client.Enabled,
                connection is not null && !connection.NeedsReconnect, connection?.NeedsReconnect ?? false, connection?.AccountName, client.QueryHint));
        }
        return result;
    }

    public Result<string, ApplicationError> CreateAuthorizeUrl(Guid playerId, string provider, string returnPath)
    {
        if (!_clients.TryGetValue(provider, out var client)) return TrackerErrors.UnknownTracker;
        if (!client.Enabled) return TrackerErrors.NotConfigured;
        var state = protector.ProtectState(JsonSerializer.Serialize(new OAuthState(playerId, client.Provider, SafePath(returnPath))), StateLifetime);
        return client.AuthorizeUrl(state);
    }

    /// <summary>Finishes the OAuth sign-in; returns the page to go back to.</summary>
    public async Task<Result<string, ApplicationError>> CompleteAsync(Guid playerId, string provider, string code, string state, CancellationToken cancellationToken)
    {
        if (!_clients.TryGetValue(provider, out var client)) return TrackerErrors.UnknownTracker;
        if (!client.Enabled) return TrackerErrors.NotConfigured;
        var json = protector.UnprotectState(state);
        var parsed = json is null ? null : JsonSerializer.Deserialize<OAuthState>(json);
        if (parsed is null || parsed.PlayerId != playerId || !string.Equals(parsed.Provider, client.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return TrackerErrors.InvalidState;
        }

        TrackerTokens tokens;
        try
        {
            tokens = await client.ExchangeCodeAsync(code, cancellationToken);
        }
        catch (Exception exception) when (exception is TrackerApiException or HttpRequestException or JsonException)
        {
            return TrackerErrors.InvalidState;
        }

        var connection = await connections.GetAsync(playerId, client.Provider, cancellationToken);
        if (connection is null)
        {
            connection = TrackerConnection.Create(playerId, client.Provider);
            connections.Add(connection);
        }
        Store(connection, tokens);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return parsed.ReturnPath;
    }

    public async Task<bool> DisconnectAsync(Guid playerId, string provider, CancellationToken cancellationToken)
    {
        var connection = await connections.GetAsync(playerId, provider, cancellationToken);
        if (connection is null) return false;
        connections.Remove(connection);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<Result<IReadOnlyList<TrackerSource>, ApplicationError>> GetSourcesAsync(Guid playerId, string provider, CancellationToken cancellationToken) =>
        CallAsync(playerId, provider, (client, token) => client.GetSourcesAsync(token, cancellationToken), cancellationToken);

    public async Task<Result<IReadOnlyList<TrackerPreviewIssue>, ApplicationError>> PreviewAsync(
        Guid playerId, string provider, Guid gameId, string sourceId, string? query, CancellationToken cancellationToken)
    {
        var issues = await CallAsync(playerId, provider, (client, token) => client.GetIssuesAsync(token, sourceId, query, MaxImport, cancellationToken), cancellationToken);
        if (!issues.IsSuccess) return issues.Error!;
        var existing = await ExistingKeysAsync(gameId, cancellationToken);
        return issues.Value!.Select(issue => new TrackerPreviewIssue(issue, existing.Contains(issue.Key))).ToList();
    }

    /// <summary>Imports the chosen issues (all of the source when <paramref name="keys"/> is empty); skips those already in the game.</summary>
    public async Task<Result<TrackerImportResult, ApplicationError>> ImportAsync(
        Guid playerId, string provider, Guid gameId, string sourceId, string? query, IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var issues = await CallAsync(playerId, provider, (client, token) => client.GetIssuesAsync(token, sourceId, query, MaxImport, cancellationToken), cancellationToken);
        if (!issues.IsSuccess) return issues.Error!;

        var gameTickets = (await tickets.GetByGame(gameId, cancellationToken)).ToList();
        var existing = gameTickets.Where(t => t.ExternalKey is not null).Select(t => t.ExternalKey!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var position = gameTickets.Select(t => t.Position).DefaultIfEmpty(-1).Max() + 1;
        var imported = 0;
        var skipped = 0;
        foreach (var issue in issues.Value!.Where(i => keys.Count == 0 || keys.Contains(i.Key)))
        {
            if (!existing.Add(issue.Key))
            {
                skipped++;
                continue;
            }

            var name = $"{issue.Key} {issue.Title}".Trim();
            var ticket = Ticket.Create(gameId, name.Length > 255 ? name[..255] : name, issue.Description.Length > 4080 ? issue.Description[..4080] : issue.Description, position++);
            ticket.LinkTo(issue.Key, issue.Url, _clients[provider].Provider);
            tickets.Add(ticket);
            imported++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TrackerImportResult(imported, skipped);
    }

    /// <summary>Writes the final estimate (or, without one, the most common card) to the issue.</summary>
    public async Task<Result<TrackerSaveResult, ApplicationError>> SaveEstimateAsync(Guid playerId, Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket?.ExternalKey is null || ticket.ExternalProvider is null || !_clients.TryGetValue(ticket.ExternalProvider, out var client))
        {
            return TrackerErrors.NotLinked;
        }

        if (!ticket.VotesRevealed) return TrackerErrors.NoEstimate;
        var values = (await votes.GetByTicket(ticketId, null, cancellationToken)).Select(v => v.Value).ToList();
        var estimate = ticket.FinalEstimate ?? TicketStats.From(true, values).Estimate;
        if (string.IsNullOrWhiteSpace(estimate)) return TrackerErrors.NoEstimate;
        if (client.CheckEstimate(estimate) is { } reason) return TrackerErrors.Rejected(reason);

        var saved = await CallAsync(playerId, client.Provider, async (c, token) =>
        {
            await c.SaveEstimateAsync(token, ticket.ExternalKey, ticket.ExternalUrl ?? string.Empty, estimate, cancellationToken);
            return true;
        }, cancellationToken);
        if (!saved.IsSuccess) return saved.Error!;
        return new TrackerSaveResult(ticket.ExternalKey, estimate, ticket.ExternalUrl ?? string.Empty);
    }

    /// <summary>Numbers as the trackers expect them ("5", "0.5").</summary>
    public static bool TryNumber(string value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

    private async Task<HashSet<string>> ExistingKeysAsync(Guid gameId, CancellationToken cancellationToken) =>
        (await tickets.GetByGame(gameId, cancellationToken)).Where(t => t.ExternalKey is not null).Select(t => t.ExternalKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private void Store(TrackerConnection connection, TrackerTokens tokens) =>
        connection.SetTokens(protector.Protect(tokens.AccessToken), tokens.RefreshToken is null ? null : protector.Protect(tokens.RefreshToken),
            tokens.ExpiresOn, tokens.AccountName);

    /// <summary>Runs a tracker call with a valid access token, refreshing it once when it expired or was rejected.</summary>
    private async Task<Result<T, ApplicationError>> CallAsync<T>(
        Guid playerId, string provider, Func<ITrackerClient, string, Task<T>> call, CancellationToken cancellationToken)
    {
        if (!_clients.TryGetValue(provider, out var client)) return TrackerErrors.UnknownTracker;
        if (!client.Enabled) return TrackerErrors.NotConfigured;
        var connection = await connections.GetAsync(playerId, client.Provider, cancellationToken);
        if (connection is null) return TrackerErrors.NotConnected;
        if (connection.NeedsReconnect) return TrackerErrors.Reconnect;

        try
        {
            if (connection.AccessTokenExpiresOn is { } expires && expires <= DateTimeOffset.UtcNow.AddMinutes(1) && !await RefreshAsync(client, connection, cancellationToken))
            {
                return TrackerErrors.Reconnect;
            }

            try
            {
                return await call(client, protector.Unprotect(connection.ProtectedAccessToken));
            }
            catch (TrackerApiException exception) when (exception.Unauthorized)
            {
                if (!await RefreshAsync(client, connection, cancellationToken)) return TrackerErrors.Reconnect;
                return await call(client, protector.Unprotect(connection.ProtectedAccessToken));
            }
        }
        catch (TrackerApiException exception) when (exception.Unauthorized)
        {
            connection.MarkNeedsReconnect();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return TrackerErrors.Reconnect;
        }
        catch (Exception exception) when (exception is TrackerApiException or HttpRequestException or JsonException or TaskCanceledException)
        {
            return TrackerErrors.Failed;
        }
    }

    private async Task<bool> RefreshAsync(ITrackerClient client, TrackerConnection connection, CancellationToken cancellationToken)
    {
        if (connection.ProtectedRefreshToken is null)
        {
            connection.MarkNeedsReconnect();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        TrackerTokens? tokens;
        try
        {
            tokens = await client.RefreshAsync(protector.Unprotect(connection.ProtectedRefreshToken), cancellationToken);
        }
        catch (TrackerApiException)
        {
            tokens = null;
        }

        if (tokens is null)
        {
            connection.MarkNeedsReconnect();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        Store(connection, tokens);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Only paths inside the app (no other hosts) can be the page to return to.</summary>
    private static string SafePath(string path) =>
        path.StartsWith('/') && !path.StartsWith("//") && !path.Contains('\\') ? path : "/game";
}
