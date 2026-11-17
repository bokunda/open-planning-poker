using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Trackers;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Integrations;

/// <summary>An issue tracker next to Jira (GitHub, Linear, Azure DevOps) and the caller's connection to it.</summary>
public sealed record TrackerInfo(string Provider, string Name, bool Enabled, bool Connected, bool NeedsReconnect, string? AccountName, string QueryHint);
public sealed record TrackerSourceInfo(string Id, string Name);
public sealed record TrackerIssuePreview(string Key, string Title, string Description, string Url, double? Estimate, bool AlreadyInGame);

// Object results, as for Jira: with query conventions an error result is a union of object types.
public sealed record TrackerAuthorizeLink(string Url);
public sealed record TrackerSourceList(List<TrackerSourceInfo> Items);
public sealed record TrackerIssueList(List<TrackerIssuePreview> Issues, int Max);
public sealed record TrackerConnected(string ReturnPath);
public sealed record TrackerImportSummary(int Imported, int Skipped);
public sealed record TrackerEstimateSaved(string IssueKey, string Value, string Url);

[QueryType]
public class TrackerQueries
{
    /// <summary>The trackers this server offers, with the caller's connection to each.</summary>
    public async Task<List<TrackerInfo>> GetTrackersAsync(
        [Service] TrackerService trackers,
        [Service] IGameAccessService gameAccess,
        CancellationToken cancellationToken = default)
    {
        var playerId = gameAccess.EnsureAuthenticated() is null ? gameAccess.CurrentUserId : Guid.Empty;
        return [.. (await trackers.GetStatusAsync(playerId, cancellationToken))
            .Select(s => new TrackerInfo(s.Provider, s.Name, s.Enabled, s.Connected, s.NeedsReconnect, s.AccountName, s.QueryHint))];
    }

    /// <summary>The tracker's consent page; it returns to /integrations/tracker/{provider}/callback, then to returnPath.</summary>
    public FieldResult<TrackerAuthorizeLink, ApplicationError> GetTrackerAuthorizeUrl(
        [Service] TrackerService trackers,
        [Service] IGameAccessService gameAccess,
        [Required] string provider,
        [Required] string returnPath)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var result = trackers.CreateAuthorizeUrl(gameAccess.CurrentUserId, provider, returnPath);
        return result.IsSuccess ? new TrackerAuthorizeLink(result.Value!) : result.Error!;
    }

    /// <summary>Repositories, teams or projects to import from.</summary>
    public async Task<FieldResult<TrackerSourceList, ApplicationError>> GetTrackerSourcesAsync(
        [Service] TrackerService trackers,
        [Service] IGameAccessService gameAccess,
        [Required] string provider,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var result = await trackers.GetSourcesAsync(gameAccess.CurrentUserId, provider, cancellationToken);
        return result.IsSuccess ? new TrackerSourceList([.. result.Value!.Select(s => new TrackerSourceInfo(s.Id, s.Name))]) : result.Error!;
    }

    /// <summary>Import preview: up to 100 issues of a source, marking those already in the game. Host and co-hosts.</summary>
    public async Task<FieldResult<TrackerIssueList, ApplicationError>> GetTrackerIssuesAsync(
        [Service] TrackerService trackers,
        [Service] IGameAccessService gameAccess,
        [Required] string provider,
        [Required] Guid gameId,
        [Required] string sourceId,
        string? query,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError) return accessError;
        var result = await trackers.PreviewAsync(gameAccess.CurrentUserId, provider, gameId, sourceId, query, cancellationToken);
        return result.IsSuccess
            ? new TrackerIssueList([.. result.Value!.Select(p => new TrackerIssuePreview(p.Issue.Key, p.Issue.Title, p.Issue.Description, p.Issue.Url, p.Issue.Estimate, p.AlreadyInGame))], TrackerService.MaxImport)
            : result.Error!;
    }
}

[MutationType]
public class TrackerMutations
{
    /// <summary>Finishes the OAuth sign-in with the code and state from the callback.</summary>
    public async Task<FieldResult<TrackerConnected, ApplicationError>> CompleteTrackerConnectionAsync(
        [Service] TrackerService trackers,
        [Service] IGameAccessService gameAccess,
        [Required] string provider,
        [Required] string code,
        [Required] string state,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        var result = await trackers.CompleteAsync(gameAccess.CurrentUserId, provider, code, state, cancellationToken);
        return result.IsSuccess ? new TrackerConnected(result.Value!) : result.Error!;
    }

    /// <summary>Deletes the caller's tokens for the tracker.</summary>
    public async Task<FieldResult<bool, ApplicationError>> DisconnectTrackerAsync(
        [Service] TrackerService trackers,
        [Service] IGameAccessService gameAccess,
        [Required] string provider,
        CancellationToken cancellationToken = default)
    {
        if (gameAccess.EnsureAuthenticated() is { } accessError) return accessError;
        return await trackers.DisconnectAsync(gameAccess.CurrentUserId, provider, cancellationToken);
    }

    /// <summary>Imports issues as tickets (all of the source when keys is empty), linked for "Save to KEY". Host and co-hosts.</summary>
    public async Task<FieldResult<TrackerImportSummary, ApplicationError>> ImportTrackerIssuesAsync(
        [Service] TrackerService trackers,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] string provider,
        [Required] Guid gameId,
        [Required] string sourceId,
        string? query,
        List<string>? keys,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError) return accessError;
        var result = await trackers.ImportAsync(gameAccess.CurrentUserId, provider, gameId, sourceId, query, keys ?? [], cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        if (result.Value!.Imported > 0) await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, cancellationToken: cancellationToken);
        return new TrackerImportSummary(result.Value.Imported, result.Value.Skipped);
    }

    /// <summary>Writes the agreed estimate to the issue the ticket came from. Host and co-hosts.</summary>
    public async Task<FieldResult<TrackerEstimateSaved, ApplicationError>> SaveEstimateToTrackerAsync(
        [Service] TrackerService trackers,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        if ((await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken)).Error is { } accessError) return accessError;
        var result = await trackers.SaveEstimateAsync(gameAccess.CurrentUserId, ticketId, cancellationToken);
        return result.IsSuccess ? new TrackerEstimateSaved(result.Value!.IssueKey, result.Value.Value, result.Value.Url) : result.Error!;
    }
}
