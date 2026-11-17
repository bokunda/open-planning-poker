namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Teams;

/// <summary>How one planning session went (OPP-134). Team numbers only.</summary>
public sealed class SprintStats
{
    public int Revealed { get; set; }
    public int FirstRoundConsensus { get; set; }

    /// <summary>Share (0–1) of revealed tickets agreed on the first vote.</summary>
    public double? FirstRoundConsensusShare { get; set; }
    public double? ReVotesPerTicket { get; set; }

    /// <summary>Average seconds from the reveal to the final estimate.</summary>
    public double? AverageDiscussionSeconds { get; set; }
    public int NeedsInfo { get; set; }

    public static SprintStats? From(SessionStats? s) => s is null ? null : new()
    {
        Revealed = s.Revealed,
        FirstRoundConsensus = s.FirstRoundConsensus,
        FirstRoundConsensusShare = s.FirstRoundConsensusShare,
        ReVotesPerTicket = s.ReVotesPerTicket,
        AverageDiscussionSeconds = s.AverageDiscussionSeconds,
        NeedsInfo = s.NeedsInfo,
    };
}

public sealed record CardAccuracyInfo(string Value, int Count, double MedianDays, double MinDays, double MaxDays);
public sealed record AccuracyOutlierInfo(TeamTicket Ticket, double Days, double CardMedianDays);

/// <summary>Estimate accuracy from Jira cycle times (OPP-135).</summary>
public sealed record TeamAccuracyInfo(List<CardAccuracyInfo> Cards, List<AccuracyOutlierInfo> Outliers, int Measured, int Pending);

public sealed record SessionSummaryInfo(string Text, string? Destination);

[ExtendObjectType(typeof(Sprint))]
public sealed class SprintStatsField
{
    /// <summary>Consensus, re-votes, discussion time and needs-info tickets of the session.</summary>
    public SprintStats? GetStats([Parent] Sprint sprint) => sprint.Stats;
}

[ExtendObjectType(typeof(Team))]
public sealed class TeamInsightsFields
{
    /// <summary>The incoming webhook for session summaries. Owner only; null for others.</summary>
    public async Task<string?> GetSummaryWebhookAsync(
        [Parent] Team team, [Service] IGameAccessService gameAccess, [Service] InsightsService insights, CancellationToken cancellationToken) =>
        await gameAccess.EnsureTeamOwnerAsync(team.Id, cancellationToken) is null ? await insights.GetWebhookAsync(team.Id, cancellationToken) : null;
}

[QueryType]
public class InsightsQueries
{
    /// <summary>Cycle times per card from Jira, with the caller's Jira connection. Team members.</summary>
    public async Task<FieldResult<TeamAccuracyInfo, ApplicationError>> GetTeamAccuracyAsync(
        [Service] InsightsService insights,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamMemberAsync(teamId, cancellationToken) is { } accessError) return accessError;
        var result = await insights.GetAccuracyAsync(teamId, gameAccess.CurrentUserId, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        var a = result.Value!;
        return new TeamAccuracyInfo(
            [.. a.Cards.Select(c => new CardAccuracyInfo(c.Value, c.Count, c.MedianDays, c.MinDays, c.MaxDays))],
            [.. a.Outliers.Select(o => new AccuracyOutlierInfo(TeamTicket.From(o.Ticket), o.Days, o.CardMedianDays))],
            a.Measured, a.Pending);
    }

    /// <summary>Preview of the session summary and where it would go. Host and co-hosts.</summary>
    public async Task<FieldResult<SessionSummaryInfo, ApplicationError>> GetSessionSummaryAsync(
        [Service] InsightsService insights,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError) return accessError;
        var result = await insights.GetSummaryAsync(gameId, cancellationToken);
        return result.IsSuccess ? new SessionSummaryInfo(result.Value!.Text, result.Value.Destination) : result.Error!;
    }
}

[MutationType]
public class InsightsMutations
{
    /// <summary>Posts the session summary to the game's Slack thread or the team's webhook. Host and co-hosts.</summary>
    public async Task<FieldResult<SessionSummaryInfo, ApplicationError>> PostSessionSummaryAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] InsightsService insights,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError) return accessError;
        if (rateLimiter.TryAcquire(ActionRateLimiter.GenerateReport, gameAccess.CurrentUserId) is { } rateLimitError) return rateLimitError;
        var result = await insights.PostSummaryAsync(gameId, cancellationToken);
        return result.IsSuccess ? new SessionSummaryInfo(result.Value!.Text, result.Value.Destination) : result.Error!;
    }

    /// <summary>Sets (or clears, with null) the Slack or Teams incoming webhook for session summaries. Team owner.</summary>
    public async Task<FieldResult<bool, ApplicationError>> SetTeamSummaryWebhookAsync(
        [Service] InsightsService insights,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        string? url,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError) return accessError;
        var result = await insights.SetWebhookAsync(teamId, url, cancellationToken);
        return result.IsSuccess ? result.Value : result.Error!;
    }
}
