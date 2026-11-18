using System.Globalization;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Chat;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;

namespace OpenPlanningPoker.GameEngine.Application.Features.Teams;

/// <summary>Posts plain text to a Slack or Microsoft Teams incoming webhook.</summary>
public interface IWebhookPoster
{
    Task<bool> PostAsync(string url, string text, CancellationToken cancellationToken);
}

/// <summary>Cycle times of one card value (OPP-135). Days are from the first status change to the resolution.</summary>
public sealed record CardAccuracy(string Value, int Count, double MedianDays, double MinDays, double MaxDays);

/// <summary>A ticket whose cycle time is far from the rest of its card: worth a look in the retro.</summary>
public sealed record AccuracyOutlier(TeamTicket Ticket, double Days, double CardMedianDays);

public sealed record TeamAccuracy(IReadOnlyList<CardAccuracy> Cards, IReadOnlyList<AccuracyOutlier> Outliers, int Measured, int Pending);

/// <summary>Where the summary goes: "slack" (the game's thread), "webhook" (the team's webhook) or null.</summary>
public sealed record SessionSummary(string Text, string? Destination);

public static class InsightsErrors
{
    public static ApplicationError InvalidWebhook => new("InvalidWebhook", "Use an incoming webhook link from Slack or Microsoft Teams.");
    public static ApplicationError NoDestination => new("NoSummaryDestination", "This game has no Slack thread and its team has no webhook.");
    public static ApplicationError PostFailed => new("SummaryPostFailed", "The summary could not be posted. Check the webhook and try again.");
}

/// <summary>Estimation insights (OPP-117): accuracy against Jira cycle times and the session summary.</summary>
public sealed class InsightsService(
    IGameRepository games,
    ITicketRepository tickets,
    IVoteRepository votes,
    ITeamRepository teams,
    ITicketCycleTimeRepository cycleTimes,
    JiraService jira,
    ChatGameService chat,
    IWebhookPoster webhooks,
    IUnitOfWork unitOfWork)
{
    /// <summary>At most this many issues are read from Jira per request; the rest follow on the next visit.</summary>
    public const int MaxRefreshPerRequest = 40;
    private static readonly TimeSpan RefreshAfter = TimeSpan.FromDays(1);
    public const string AppUrl = "https://app.openplanningpoker.com";

    /// <summary>Only https links to Slack and Microsoft Teams (Office 365 connectors, Power Automate workflows) are accepted.</summary>
    public static bool IsAllowedWebhook(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return false;
        var host = uri.Host.ToLowerInvariant();
        return host == "hooks.slack.com" || host.EndsWith(".webhook.office.com") || host.EndsWith(".logic.azure.com")
            || host.EndsWith(".powerplatform.com");
    }

    public async Task<Result<bool, ApplicationError>> SetWebhookAsync(Guid teamId, string? url, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(url) && (!IsAllowedWebhook(url) || url.Length > Team.MaxWebhookLength)) return InsightsErrors.InvalidWebhook;
        var team = await teams.GetByIdAsync(teamId, cancellationToken);
        if (team is null) return new ApplicationError("TeamNotFound", "Team not found.");
        team.SetSummaryWebhook(url);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> GetWebhookAsync(Guid teamId, CancellationToken cancellationToken) =>
        (await teams.GetByIdAsync(teamId, cancellationToken))?.SummaryWebhookUrl;

    public async Task<Result<SessionSummary, ApplicationError>> GetSummaryAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await games.GetByIdAsync(gameId, cancellationToken);
        if (game is null) return GameAccessErrors.GameNotFound;
        var gameTickets = (await tickets.GetByGame(gameId, cancellationToken)).ToList();
        var gameVotes = await votes.GetByGame(gameId, null, cancellationToken);

        var estimated = 0;
        var points = 0d;
        foreach (var ticket in gameTickets.Where(t => t.IsEstimated))
        {
            var estimate = Teams.EstimateOf(ticket, [.. gameVotes.Where(v => v.TicketId == ticket.Id).Select(v => v.Value)]);
            if (estimate is null) continue;
            estimated++;
            if (double.TryParse(estimate, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) points += n;
        }

        var needsInfo = gameTickets.Where(t => t.NeedsInfo).Select(t => t.ExternalKey ?? t.Name).ToList();
        var lines = new List<string>
        {
            $"*{game.Name}* is done: {estimated} of {gameTickets.Count} tickets estimated, {points.ToString("0.#", CultureInfo.InvariantCulture)} points.",
        };
        if (needsInfo.Count > 0) lines.Add($"Needs more information: {string.Join(", ", needsInfo)}");
        lines.Add($"Results and PDF report: {AppUrl}/game/{game.Id}");

        string? destination = await chat.HasSlackThreadAsync(gameId, cancellationToken) ? "slack"
            : game.TeamId is { } teamId && await GetWebhookAsync(teamId, cancellationToken) is not null ? "webhook" : null;
        return new SessionSummary(string.Join("\n", lines), destination);
    }

    public async Task<Result<SessionSummary, ApplicationError>> PostSummaryAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var summary = await GetSummaryAsync(gameId, cancellationToken);
        if (!summary.IsSuccess) return summary.Error!;
        var s = summary.Value!;
        switch (s.Destination)
        {
            case "slack":
                return await chat.PostToThreadAsync(gameId, s.Text, cancellationToken) ? s : InsightsErrors.PostFailed;
            case "webhook":
                var game = await games.GetByIdAsync(gameId, cancellationToken);
                var url = await GetWebhookAsync(game!.TeamId!.Value, cancellationToken);
                try
                {
                    return url is not null && await webhooks.PostAsync(url, s.Text.Replace("*", string.Empty), cancellationToken) ? s : InsightsErrors.PostFailed;
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
                {
                    return InsightsErrors.PostFailed;
                }
            default:
                return InsightsErrors.NoDestination;
        }
    }

    /// <summary>Accuracy per card from the Jira cycle times of the team's estimated tickets, refreshing stale ones.</summary>
    public async Task<Result<TeamAccuracy, ApplicationError>> GetAccuracyAsync(Guid teamId, Guid playerId, CancellationToken cancellationToken)
    {
        var teamTickets = (await TeamReference.EstimatedTicketsAsync(teamId, games, tickets, votes, cancellationToken))
            .Where(t => t.ExternalKey is not null && (t.ExternalUrl?.Contains(".atlassian.net/", StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
        if (teamTickets.Count == 0) return new TeamAccuracy([], [], 0, 0);

        var cached = (await cycleTimes.GetAsync([.. teamTickets.Select(t => t.TicketId)], cancellationToken)).ToDictionary(c => c.TicketId);
        var stale = teamTickets
            .Where(t => !cached.TryGetValue(t.TicketId, out var c) || (c.DoneOn is null && c.CheckedOn < DateTimeOffset.UtcNow - RefreshAfter))
            .ToList();
        var refresh = stale.Take(MaxRefreshPerRequest).ToList();
        if (refresh.Count > 0)
        {
            var times = await jira.GetIssueTimesAsync(playerId, [.. refresh.Select(t => t.ExternalKey!).Distinct()], cancellationToken);
            if (!times.IsSuccess) return times.Error!;
            foreach (var ticket in refresh)
            {
                if (!cached.TryGetValue(ticket.TicketId, out var entry))
                {
                    entry = TicketCycleTime.Create(ticket.TicketId);
                    cycleTimes.Add(entry);
                    cached[ticket.TicketId] = entry;
                }
                var (started, done) = times.Value!.TryGetValue(ticket.ExternalKey!, out var t) ? t : (null, null);
                entry.Update(started, done);
            }
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var measured = teamTickets
            .Select(t => (ticket: t, days: cached.TryGetValue(t.TicketId, out var c) ? c.Days : null))
            .Where(m => m.days is not null)
            .Select(m => (m.ticket, days: m.days!.Value))
            .ToList();
        var cards = measured.GroupBy(m => m.ticket.Estimate)
            .Select(g =>
            {
                var days = g.Select(m => m.days).OrderBy(d => d).ToList();
                return new CardAccuracy(g.Key, days.Count, Median(days), days[0], days[^1]);
            })
            .OrderBy(c => double.TryParse(c.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : double.MaxValue)
            .ToList();
        var medians = cards.ToDictionary(c => c.Value, c => c);
        var outliers = measured
            .Where(m => medians[m.ticket.Estimate] is { Count: >= 3 } card && card.MedianDays > 0
                && (m.days > card.MedianDays * 2 || m.days < card.MedianDays / 2))
            .Select(m => new AccuracyOutlier(m.ticket, m.days, medians[m.ticket.Estimate].MedianDays))
            .OrderByDescending(o => Math.Abs(o.Days - o.CardMedianDays))
            .Take(10)
            .ToList();
        return new TeamAccuracy(cards, outliers, measured.Count, Math.Max(0, stale.Count - refresh.Count));
    }

    private static double Median(IReadOnlyList<double> sorted) =>
        sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : Math.Round((sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2, 1);
}
