using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Integrations;

/// <summary>The caller's Jira connection. Enabled is false when the server has no Atlassian app configured.</summary>
public sealed class JiraConnectionStatus
{
    public bool Enabled { get; init; }
    public bool Connected { get; init; }
    public bool NeedsReconnect { get; init; }
    public string? SiteName { get; init; }
    public string? SiteUrl { get; init; }

    public static JiraConnectionStatus From(JiraStatus status) => new()
    {
        Enabled = status.Enabled,
        Connected = status.Connected,
        NeedsReconnect = status.NeedsReconnect,
        SiteName = status.SiteName,
        SiteUrl = status.SiteUrl
    };
}

public sealed record JiraSiteInfo(string CloudId, string Name, string Url);
public sealed record JiraBoardInfo(long Id, string Name, string Type);
public sealed record JiraSprintInfo(long Id, string Name, string State);
public sealed record JiraIssuePreview(string Key, string Summary, string Description, double? StoryPoints, string Url, bool AlreadyInGame);
public sealed record JiraIssueSearch(List<JiraIssuePreview> Issues, bool Truncated, int Max);
public sealed record JiraConnectionResult(List<JiraSiteInfo> Sites, string ReturnPath, bool SiteSelected);
public sealed record JiraImportSummary(int Imported, int Skipped);
public sealed record JiraEstimateSaved(string IssueKey, string Value, string Url);

// Query results are object types: with query conventions an error result becomes a union, and a union
// can only hold object types (not a string or a list).
public sealed record JiraAuthorizeLink(string Url);
public sealed record JiraSiteList(List<JiraSiteInfo> Items);
public sealed record JiraBoardList(List<JiraBoardInfo> Items);
public sealed record JiraSprintList(List<JiraSprintInfo> Items);

[QueryType]
public class JiraQueries
{
    /// <summary>Whether the integration is available and the caller has connected Jira.</summary>
    public async Task<JiraConnectionStatus> GetJiraConnectionAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        CancellationToken cancellationToken = default) =>
        gameAccess.EnsureAuthenticated() is not null
            ? new JiraConnectionStatus { Enabled = jira.Enabled }
            : JiraConnectionStatus.From(await jira.GetStatusAsync(gameAccess.CurrentUserId, cancellationToken));

    /// <summary>The Atlassian consent page to open; it returns to the app's Jira callback, then to returnPath.</summary>
    public FieldResult<JiraAuthorizeLink, ApplicationError> GetJiraAuthorizeUrl(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        [Required] string returnPath)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var result = jira.CreateAuthorizeUrl(gameAccess.CurrentUserId, returnPath);
        return result.IsSuccess ? new JiraAuthorizeLink(result.Value!) : result.Error!;
    }

    public async Task<FieldResult<JiraSiteList, ApplicationError>> GetJiraSitesAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var result = await jira.GetSitesAsync(gameAccess.CurrentUserId, cancellationToken);
        return result.IsSuccess ? new JiraSiteList([.. result.Value!.Select(s => new JiraSiteInfo(s.CloudId, s.Name, s.Url))]) : result.Error!;
    }

    public async Task<FieldResult<JiraBoardList, ApplicationError>> GetJiraBoardsAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var result = await jira.GetBoardsAsync(gameAccess.CurrentUserId, cancellationToken);
        return result.IsSuccess ? new JiraBoardList([.. result.Value!.Select(b => new JiraBoardInfo(b.Id, b.Name, b.Type))]) : result.Error!;
    }

    public async Task<FieldResult<JiraSprintList, ApplicationError>> GetJiraSprintsAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        long boardId,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var result = await jira.GetSprintsAsync(gameAccess.CurrentUserId, boardId, cancellationToken);
        return result.IsSuccess ? new JiraSprintList([.. result.Value!.Select(s => new JiraSprintInfo(s.Id, s.Name, s.State))]) : result.Error!;
    }

    /// <summary>
    /// Import preview: a sprint's issues, a board's open issues, or a JQL query, with the issues that are already
    /// tickets of the game marked. Host or co-host only.
    /// </summary>
    public async Task<FieldResult<JiraIssueSearch, ApplicationError>> GetJiraIssuesAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        long? boardId,
        long? sprintId,
        string? jql,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError) return accessError;
        var result = await jira.PreviewAsync(gameAccess.CurrentUserId, gameId, boardId, sprintId, jql, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        var issues = result.Value!.Issues
            .Select(item => new JiraIssuePreview(item.Issue.Key, item.Issue.Summary, item.Issue.Description, item.Issue.StoryPoints, item.Issue.Url, item.AlreadyInGame))
            .ToList();
        return new JiraIssueSearch(issues, result.Value.Truncated, JiraService.MaxImport);
    }
}

[MutationType]
public class JiraMutations
{
    /// <summary>Finishes the Atlassian consent: stores the tokens (encrypted) and lists the caller's sites.</summary>
    public async Task<FieldResult<JiraConnectionResult, ApplicationError>> CompleteJiraConnectionAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        [Required] string code,
        [Required] string state,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var result = await jira.CompleteAsync(gameAccess.CurrentUserId, code, state, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        return new JiraConnectionResult(
            [.. result.Value!.Sites.Select(s => new JiraSiteInfo(s.CloudId, s.Name, s.Url))],
            result.Value.ReturnPath,
            result.Value.SiteSelected);
    }

    public async Task<FieldResult<JiraConnectionStatus, ApplicationError>> SelectJiraSiteAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        [Required] string cloudId,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var result = await jira.SelectSiteAsync(gameAccess.CurrentUserId, cloudId, cancellationToken);
        return result.IsSuccess ? JiraConnectionStatus.From(result.Value!) : result.Error!;
    }

    /// <summary>Deletes the caller's stored Jira tokens.</summary>
    public async Task<FieldResult<bool, ApplicationError>> DisconnectJiraAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        return await jira.DisconnectAsync(gameAccess.CurrentUserId, cancellationToken);
    }

    /// <summary>Adds Jira issues as tickets; issues already in the game are skipped. Host or co-host only.</summary>
    public async Task<FieldResult<JiraImportSummary, ApplicationError>> ImportJiraIssuesAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        [Service] ITopicEventSender eventSender,
        [Required] Guid gameId,
        [Required] List<string> issueKeys,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError) return accessError;
        var result = await jira.ImportAsync(gameAccess.CurrentUserId, gameId, issueKeys, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        if (result.Value!.Imported > 0)
        {
            await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, cancellationToken: cancellationToken);
        }
        return new JiraImportSummary(result.Value.Imported, result.Value.Skipped);
    }

    /// <summary>
    /// Writes the final estimate (or the most common card) to the Jira issue's story points; optionally adds a
    /// comment with the votes. Host or co-host only, after the reveal.
    /// </summary>
    public async Task<FieldResult<JiraEstimateSaved, ApplicationError>> SaveEstimateToJiraAsync(
        [Service] JiraService jira,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        bool addComment,
        CancellationToken cancellationToken = default)
    {
        var (_, accessError) = await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null) return accessError;
        var result = await jira.SaveEstimateAsync(gameAccess.CurrentUserId, ticketId, addComment, cancellationToken);
        return result.IsSuccess ? new JiraEstimateSaved(result.Value!.IssueKey, result.Value.Value, result.Value.Url) : result.Error!;
    }
}
