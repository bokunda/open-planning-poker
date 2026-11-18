using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using OpenPlanningPoker.GameEngine.Domain.Integrations;

namespace OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;

public sealed record JiraStatus(bool Enabled, bool Connected, bool NeedsReconnect, string? SiteName, string? SiteUrl);
public sealed record JiraConnectResult(IReadOnlyList<JiraSite> Sites, string ReturnPath, bool SiteSelected);
public sealed record JiraPreviewIssue(JiraIssue Issue, bool AlreadyInGame);
public sealed record JiraPreview(IReadOnlyList<JiraPreviewIssue> Issues, bool Truncated);
public sealed record JiraImportResult(int Imported, int Skipped);
public sealed record JiraSaveResult(string IssueKey, string Value, string Url);

public static class JiraErrors
{
    public static readonly ApplicationError NotConfigured = new("JiraNotConfigured", "The Jira integration is not set up on this server.");
    public static readonly ApplicationError NotConnected = new("JiraNotConnected", "Connect Jira first.");
    public static readonly ApplicationError Reconnect = new("JiraReconnect", "Jira access was revoked or expired. Reconnect Jira.");
    public static readonly ApplicationError NoSite = new("JiraNoSite", "Choose a Jira site first.");
    public static readonly ApplicationError InvalidState = new("JiraInvalidState", "The Jira sign-in expired or does not belong to you. Try again.");
    public static readonly ApplicationError Forbidden = new("JiraForbidden", "Jira does not allow you to do this. Ask a Jira admin for permission.");
    public static readonly ApplicationError FieldMissing = new("JiraFieldMissing", "This issue has no Story Points field on its edit screen.");
    public static readonly ApplicationError NotNumeric = new("JiraNotNumeric", "Only numeric estimates can be saved as story points.");
    public static readonly ApplicationError NoEstimate = new("JiraNoEstimate", "Reveal the cards and choose an estimate first.");
    public static readonly ApplicationError NotLinked = new("JiraNotLinked", "This ticket was not imported from Jira.");
    public static readonly ApplicationError InvalidQuery = new("JiraInvalidQuery", "Jira could not run this query. Check the JQL.");
    public static readonly ApplicationError Failed = new("JiraFailed", "Jira did not answer as expected. Try again in a moment.");
}

/// <summary>
/// Jira Cloud integration (OPP-10, OPP-11, OPP-12): connect with OAuth 2.0, import issues, save the agreed
/// estimate. One connection per player; the access token is refreshed when it is about to expire.
/// </summary>
public sealed class JiraService(
    IOptions<JiraOptions> options,
    IJiraApi api,
    ISecretProtector protector,
    IJiraConnectionRepository connections,
    ITicketRepository tickets,
    IVoteRepository votes,
    IUnitOfWork unitOfWork)
{
    public const int MaxImport = 100;
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(15);
    private readonly JiraOptions _options = options.Value;

    public bool Enabled => _options.Enabled;

    public async Task<JiraStatus> GetStatusAsync(Guid playerId, CancellationToken cancellationToken)
    {
        if (!Enabled) return new(false, false, false, null, null);
        var connection = await connections.GetByPlayerAsync(playerId, cancellationToken);
        return connection is null
            ? new(true, false, false, null, null)
            : new(true, !connection.NeedsReconnect, connection.NeedsReconnect, connection.SiteName, connection.SiteUrl);
    }

    /// <summary>The Atlassian consent URL. The state ties the answer to this player and the page to return to.</summary>
    public Result<string, ApplicationError> CreateAuthorizeUrl(Guid playerId, string returnPath)
    {
        if (!Enabled) return JiraErrors.NotConfigured;
        var state = protector.ProtectState(JsonSerializer.Serialize(new OAuthState(playerId, SafePath(returnPath))), StateLifetime);
        var query = new Dictionary<string, string>
        {
            ["audience"] = "api.atlassian.com",
            ["client_id"] = _options.ClientId,
            ["scope"] = _options.Scopes,
            ["redirect_uri"] = _options.RedirectUri,
            ["state"] = state,
            ["response_type"] = "code",
            ["prompt"] = "consent",
        };
        return "https://auth.atlassian.com/authorize?" + string.Join('&', query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
    }

    public async Task<Result<JiraConnectResult, ApplicationError>> CompleteAsync(Guid playerId, string code, string state, CancellationToken cancellationToken)
    {
        if (!Enabled) return JiraErrors.NotConfigured;
        var json = protector.UnprotectState(state);
        var parsed = json is null ? null : JsonSerializer.Deserialize<OAuthState>(json);
        if (parsed is null || parsed.PlayerId != playerId) return JiraErrors.InvalidState;

        JiraTokens tokens;
        IReadOnlyList<JiraSite> sites;
        try
        {
            tokens = await api.ExchangeCodeAsync(code, cancellationToken);
            sites = await api.GetSitesAsync(tokens.AccessToken, cancellationToken);
        }
        catch (JiraApiException)
        {
            return JiraErrors.InvalidState;
        }

        var connection = await connections.GetByPlayerAsync(playerId, cancellationToken);
        if (connection is null)
        {
            connection = JiraConnection.Create(playerId);
            connections.Add(connection);
        }
        connection.SetTokens(protector.Protect(tokens.AccessToken), protector.Protect(tokens.RefreshToken), tokens.ExpiresOn);

        // One site: pick it right away. Several: the player chooses next.
        var selected = false;
        if (sites.Count == 1)
        {
            connection.SelectSite(sites[0].CloudId, sites[0].Name, sites[0].Url);
            selected = true;
        }
        else if (connection.CloudId is not null && sites.All(site => site.CloudId != connection.CloudId))
        {
            connection.SelectSite(string.Empty, string.Empty, string.Empty);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new JiraConnectResult(sites, parsed.ReturnPath, selected);
    }

    public async Task<Result<IReadOnlyList<JiraSite>, ApplicationError>> GetSitesAsync(Guid playerId, CancellationToken cancellationToken) =>
        await CallAsync(playerId, requireSite: false, (token, _) => api.GetSitesAsync(token, cancellationToken), cancellationToken);

    public async Task<Result<JiraStatus, ApplicationError>> SelectSiteAsync(Guid playerId, string cloudId, CancellationToken cancellationToken)
    {
        var sites = await GetSitesAsync(playerId, cancellationToken);
        if (sites.IsSuccess == false) return sites.Error!;
        var site = sites.Value!.FirstOrDefault(item => item.CloudId == cloudId);
        if (site is null) return JiraErrors.NoSite;

        var connection = (await connections.GetByPlayerAsync(playerId, cancellationToken))!;
        connection.SelectSite(site.CloudId, site.Name, site.Url);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetStatusAsync(playerId, cancellationToken);
    }

    /// <summary>Deletes the stored tokens. The player can revoke the app in their Atlassian account too.</summary>
    public async Task<bool> DisconnectAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var connection = await connections.GetByPlayerAsync(playerId, cancellationToken);
        if (connection is null) return false;
        connections.Remove(connection);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<Result<IReadOnlyList<JiraBoard>, ApplicationError>> GetBoardsAsync(Guid playerId, CancellationToken cancellationToken) =>
        CallAsync(playerId, requireSite: true, (token, c) => api.GetBoardsAsync(token, c.CloudId!, cancellationToken), cancellationToken);

    public Task<Result<IReadOnlyList<JiraSprint>, ApplicationError>> GetSprintsAsync(Guid playerId, long boardId, CancellationToken cancellationToken) =>
        CallAsync(playerId, requireSite: true, (token, c) => api.GetSprintsAsync(token, c.CloudId!, boardId, cancellationToken), cancellationToken);

    /// <summary>
    /// The issues of a sprint (or a board's backlog when no sprint is given) or of a JQL query, with the ones that
    /// are already tickets of the game marked. At most <see cref="MaxImport"/>.
    /// </summary>
    public async Task<Result<JiraPreview, ApplicationError>> PreviewAsync(
        Guid playerId, Guid gameId, long? boardId, long? sprintId, string? jql, CancellationToken cancellationToken)
    {
        var query = BuildJql(sprintId, jql);
        if (query is null && boardId is not > 0) return JiraErrors.InvalidQuery;

        var page = await CallAsync(playerId, requireSite: true, async (token, c) =>
        {
            var fields = await api.GetStoryPointFieldsAsync(token, c.CloudId!, cancellationToken);
            return query is null
                ? await api.GetBoardIssuesAsync(token, c.CloudId!, c.SiteUrl!, boardId!.Value, fields, MaxImport, cancellationToken)
                : await api.SearchAsync(token, c.CloudId!, c.SiteUrl!, query, fields, MaxImport, cancellationToken);
        }, cancellationToken);
        if (page.IsSuccess == false) return page.Error!;

        var existing = await ExistingKeysAsync(gameId, cancellationToken);
        return new JiraPreview(
            [.. page.Value!.Issues.Select(issue => new JiraPreviewIssue(issue, existing.Contains(issue.Key)))],
            page.Value!.Truncated);
    }

    /// <summary>
    /// Adds the chosen issues as tickets ("KEY Summary", the description and a link back). Issues already in the
    /// game are skipped, so importing twice creates no duplicates.
    /// </summary>
    public async Task<Result<JiraImportResult, ApplicationError>> ImportAsync(
        Guid playerId, Guid gameId, IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var wanted = keys.Select(key => key.Trim().ToUpperInvariant()).Where(key => key.Length > 0).Distinct().Take(MaxImport).ToList();
        if (wanted.Count == 0) return new JiraImportResult(0, 0);

        var jql = $"key in ({string.Join(',', wanted.Select(QuoteKey))})";
        var page = await CallAsync(playerId, requireSite: true, async (token, c) =>
            await api.SearchAsync(token, c.CloudId!, c.SiteUrl!, jql, [], MaxImport, cancellationToken), cancellationToken);
        if (page.IsSuccess == false) return page.Error!;

        var existingTickets = (await tickets.GetByGame(gameId, cancellationToken)).ToList();
        var existing = existingTickets.Where(t => t.ExternalKey is not null).Select(t => t.ExternalKey!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var position = existingTickets.Select(t => t.Position).DefaultIfEmpty(-1).Max() + 1;

        var found = page.Value!.Issues.ToDictionary(issue => issue.Key, StringComparer.OrdinalIgnoreCase);
        var imported = 0;
        foreach (var key in wanted)
        {
            if (existing.Contains(key) || !found.TryGetValue(key, out var issue)) continue;
            var name = Truncate($"{issue.Key} {issue.Summary}", 255);
            var description = Truncate(issue.Description, 4080);
            var ticket = Ticket.Create(gameId, name, description, position++);
            ticket.LinkTo(issue.Key, issue.Url);
            tickets.Add(ticket);
            imported++;
        }

        if (imported > 0) await unitOfWork.SaveChangesAsync(cancellationToken);
        return new JiraImportResult(imported, wanted.Count - imported);
    }

    /// <summary>
    /// Writes the final estimate (or, without one, the most common card) to the issue's story points field, and
    /// optionally a comment with the vote distribution. Nothing is saved when anything is missing.
    /// </summary>
    public async Task<Result<JiraSaveResult, ApplicationError>> SaveEstimateAsync(
        Guid playerId, Guid ticketId, bool addComment, CancellationToken cancellationToken)
    {
        var ticket = await tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket?.ExternalKey is null) return JiraErrors.NotLinked;
        if (!ticket.IsEstimated) return JiraErrors.NoEstimate;

        var ticketVotes = await votes.GetByTicket(ticketId, null, cancellationToken);
        var values = ticketVotes.Select(vote => vote.Value).ToList();
        // Notes of the lowest and highest voters go into the comment too (OPP-122).
        var noteLines = ticketVotes.Where(vote => vote.Note is not null).Select(vote => $"Note ({vote.Value}): {vote.Note}").ToList();
        var stats = TicketStats.From(true, values);
        var estimate = ticket.FinalEstimate ?? stats.Estimate;
        if (string.IsNullOrWhiteSpace(estimate)) return JiraErrors.NoEstimate;
        if (!double.TryParse(estimate, NumberStyles.Float, CultureInfo.InvariantCulture, out var points)) return JiraErrors.NotNumeric;

        var key = ticket.ExternalKey;
        var result = await CallAsync(playerId, requireSite: true, async (token, c) =>
        {
            var candidates = await api.GetStoryPointFieldsAsync(token, c.CloudId!, cancellationToken);
            var editable = await api.GetEditableFieldsAsync(token, c.CloudId!, key, cancellationToken);
            var field = candidates.FirstOrDefault(item => editable.Contains(item.Id));
            if (field is null) return false;

            await api.SetFieldAsync(token, c.CloudId!, key, field.Id, points, cancellationToken);
            if (addComment)
            {
                var distribution = string.Join(", ", stats.Distribution.Select(count => count.Count == 1 ? count.Value : $"{count.Value} ×{count.Count}"));
                await api.AddCommentAsync(token, c.CloudId!, key,
                    string.Join("\n", new[] { $"Estimated with Open Planning Poker: {estimate}", $"Votes: {distribution}" }.Concat(noteLines)),
                    cancellationToken);
            }
            return true;
        }, cancellationToken);

        if (result.IsSuccess == false) return result.Error!;
        if (!result.Value) return JiraErrors.FieldMissing;
        return new JiraSaveResult(key, estimate, ticket.ExternalUrl ?? string.Empty);
    }

    /// <summary>Start and resolution times of issues (OPP-135), with the caller's Jira connection.</summary>
    public async Task<Result<IReadOnlyDictionary<string, (DateTimeOffset? Started, DateTimeOffset? Done)>, ApplicationError>> GetIssueTimesAsync(
        Guid playerId, IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var result = await CallAsync(playerId, requireSite: true, async (token, c) =>
        {
            var times = new Dictionary<string, (DateTimeOffset?, DateTimeOffset?)>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                try
                {
                    times[key] = await api.GetIssueTimesAsync(token, c.CloudId!, key, cancellationToken);
                }
                catch (JiraApiException exception) when (!exception.Unauthorized)
                {
                    // Deleted or hidden issue: leave it out.
                }
            }
            return (IReadOnlyDictionary<string, (DateTimeOffset? Started, DateTimeOffset? Done)>)times;
        }, cancellationToken);
        return result;
    }

    /// <summary>Runs a Jira call with a valid access token, refreshing it once when it expired or was rejected.</summary>
    private async Task<Result<T, ApplicationError>> CallAsync<T>(
        Guid playerId, bool requireSite, Func<string, JiraConnection, Task<T>> call, CancellationToken cancellationToken)
    {
        if (!Enabled) return JiraErrors.NotConfigured;
        var connection = await connections.GetByPlayerAsync(playerId, cancellationToken);
        if (connection is null) return JiraErrors.NotConnected;
        if (connection.NeedsReconnect) return JiraErrors.Reconnect;
        if (requireSite && string.IsNullOrEmpty(connection.CloudId)) return JiraErrors.NoSite;

        try
        {
            if (connection.AccessTokenExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(1))
            {
                var refreshed = await RefreshAsync(connection, cancellationToken);
                if (refreshed is not null) return refreshed;
            }

            try
            {
                return await call(protector.Unprotect(connection.ProtectedAccessToken), connection);
            }
            catch (JiraApiException ex) when (ex.Unauthorized)
            {
                var refreshed = await RefreshAsync(connection, cancellationToken);
                if (refreshed is not null) return refreshed;
                return await call(protector.Unprotect(connection.ProtectedAccessToken), connection);
            }
        }
        catch (JiraApiException ex) when (ex.Forbidden) { return JiraErrors.Forbidden; }
        catch (JiraApiException ex) when (ex.Status == 400) { return JiraErrors.InvalidQuery; }
        catch (JiraApiException ex) when (ex.Unauthorized) { return JiraErrors.Reconnect; }
        catch (JiraApiException) { return JiraErrors.Failed; }
        catch (HttpRequestException) { return JiraErrors.Failed; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return JiraErrors.Failed; }
    }

    /// <summary>New tokens (Atlassian rotates the refresh token). A rejected refresh means "reconnect".</summary>
    private async Task<ApplicationError?> RefreshAsync(JiraConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await api.RefreshAsync(protector.Unprotect(connection.ProtectedRefreshToken), cancellationToken);
            connection.SetTokens(protector.Protect(tokens.AccessToken), protector.Protect(tokens.RefreshToken), tokens.ExpiresOn);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (JiraApiException ex) when (ex.Status is 400 or 401 or 403)
        {
            connection.MarkNeedsReconnect();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return JiraErrors.Reconnect;
        }
    }

    private async Task<HashSet<string>> ExistingKeysAsync(Guid gameId, CancellationToken cancellationToken) =>
        (await tickets.GetByGame(gameId, cancellationToken))
            .Where(ticket => ticket.ExternalKey is not null)
            .Select(ticket => ticket.ExternalKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>A sprint's issues or the player's own JQL (searched as typed); null for a whole board.</summary>
    internal static string? BuildJql(long? sprintId, string? jql)
    {
        if (sprintId is > 0) return $"sprint = {sprintId} ORDER BY Rank ASC";
        if (!string.IsNullOrWhiteSpace(jql)) return jql.Trim();
        return null;
    }

    private static string QuoteKey(string key) =>
        System.Text.RegularExpressions.Regex.IsMatch(key, "^[A-Z][A-Z0-9_]*-[0-9]+$") ? key : $"\"{key.Replace("\"", string.Empty)}\"";

    /// <summary>Only paths inside the app (a game or the team page) are valid return targets.</summary>
    internal static string SafePath(string path) =>
        path.StartsWith('/') && !path.StartsWith("//") && !path.Contains('\\') ? path : "/";

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";

    private sealed record OAuthState(Guid PlayerId, string ReturnPath);
}
