using System.Globalization;
using System.Text;
using OpenPlanningPoker.GameEngine.Application.Localization;
using OpenPlanningPoker.Shared.Services;

namespace OpenPlanningPoker.GameEngine.Application.Features.Teams;

/// <summary>
/// A card as one viewer may see it. While writing, the cards of others are not listed at all (only counted per column).
/// <see cref="AuthorId"/> is set only when the host shows names; <see cref="Votes"/> only after voting ended.
/// </summary>
public sealed record RetroCardItem(
    Guid Id, int Column, string Text, bool Mine, Guid? AuthorId, Guid? GroupId, string? GroupTitle, int MyVotes, int? Votes);

public sealed record RetroActionItemResponse(Guid Id, Guid RetroId, Guid? CardId, string Text, Guid? OwnerId, bool Done, DateTimeOffset CreatedOn);

/// <summary>The retro of a sprint as one viewer sees it.</summary>
public sealed record RetroView(
    Guid Id,
    Guid TeamId,
    Guid GameId,
    string Template,
    int Columns,
    RetroPhase Phase,
    bool ShowAuthors,
    int DotsPerPerson,
    int DotsLeft,
    IReadOnlyList<int> CountPerColumn,
    IReadOnlyList<RetroCardItem> Cards,
    IReadOnlyList<RetroActionItemResponse> ActionItems,
    IReadOnlyList<RetroActionItemResponse> CarriedOver);

/// <summary>Estimation facts of the sprint for the "Sprint in numbers" panel (OPP-167).</summary>
public sealed record SprintFacts(
    bool HasData,
    double EstimatedPoints,
    int EstimatedTickets,
    int TicketCount,
    double? PlannedPoints,
    IReadOnlyList<VelocityPoint> Velocity,
    IReadOnlyList<SprintFactTicket> Outliers,
    IReadOnlyList<SprintFactTicket> LowConfidence);

public sealed record VelocityPoint(Guid GameId, string Name, double Points, bool Current);

/// <summary>A ticket worth a talk: an outlier in cycle time, or one the team was not sure about.</summary>
public sealed record SprintFactTicket(Guid TicketId, string Name, string? ExternalKey, string? Estimate, double? Days, double? CardMedianDays, double? Confidence);

public static class RetroErrors
{
    public static ApplicationError NotFound => new("RetroNotFound", "This sprint has no retro yet.");
    public static ApplicationError AlreadyStarted => new("RetroAlreadyStarted", "This sprint already has a retro.");
    public static ApplicationError NotInTeam => new("SprintNotInTeam", "This planning session does not belong to the team.");
    public static ApplicationError UnknownTemplate => new("UnknownRetroTemplate", "Choose one of the retro templates.");
    public static ApplicationError WrongPhase => new("RetroWrongPhase", "That is not possible in this step of the retro.");
    public static ApplicationError InvalidColumn => new("InvalidRetroColumn", "Choose one of the board's columns.");
    public static ApplicationError InvalidText => new("InvalidRetroText", $"Write 1 to {RetroCard.MaxTextLength} characters.");
    public static ApplicationError CardNotFound => new("RetroCardNotFound", "Card not found.");
    public static ApplicationError NotYourCard => new("NotYourRetroCard", "Only the author can change this card.");
    public static ApplicationError NoDotsLeft => new("NoDotsLeft", "You have used all your dots.");
    public static ApplicationError ActionItemNotFound => new("ActionItemNotFound", "Action item not found.");
    public static ApplicationError InvalidOwner => new("InvalidActionItemOwner", "The owner must be a member of the team.");
}

/// <summary>Sprint retrospectives in the team room (OPP-162).</summary>
public sealed class RetroService(
    IRetroRepository retros,
    IGameRepository games,
    IGamePlayerRepository players,
    ITeamRepository teams,
    ITicketRepository tickets,
    IVoteRepository votes,
    ITicketCycleTimeRepository cycleTimes,
    ISender sender,
    IWebhookPoster webhooks,
    IUserService users,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Retro, ApplicationError>> StartAsync(Guid teamId, Guid gameId, string template, CancellationToken cancellationToken)
    {
        if (!Retro.Templates.ContainsKey(template)) return RetroErrors.UnknownTemplate;
        var game = await games.GetByIdAsync(gameId, cancellationToken);
        if (game is null || game.TeamId != teamId) return RetroErrors.NotInTeam;
        if (await retros.GetByGameAsync(gameId, cancellationToken) is not null) return RetroErrors.AlreadyStarted;

        var retro = Retro.Create(teamId, gameId, template);
        retros.Add(retro);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return retro;
    }

    public Task<Retro?> GetByGameAsync(Guid gameId, CancellationToken cancellationToken) => retros.GetByGameAsync(gameId, cancellationToken);

    public Task<Retro?> GetAsync(Guid retroId, CancellationToken cancellationToken) => retros.GetAsync(retroId, cancellationToken);

    public Task<RetroCard?> GetCardAsync(Guid cardId, CancellationToken cancellationToken) => retros.GetCardAsync(cardId, cancellationToken);

    public Task<RetroActionItem?> GetActionItemAsync(Guid id, CancellationToken cancellationToken) => retros.GetActionItemAsync(id, cancellationToken);

    /// <summary>Retros of the team by sprint, for the team room.</summary>
    public async Task<IReadOnlyDictionary<Guid, Retro>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken) =>
        (await retros.GetByTeamAsync(teamId, cancellationToken)).ToDictionary(r => r.GameId);

    public async Task<RetroView> ViewAsync(Retro retro, Guid viewerId, CancellationToken cancellationToken)
    {
        var cards = await retros.GetCardsAsync(retro.Id, cancellationToken);
        var allVotes = await retros.GetVotesAsync(retro.Id, cancellationToken);
        var items = await retros.GetActionItemsAsync(retro.TeamId, cancellationToken);
        return Build(retro, cards, allVotes, items, viewerId);
    }

    /// <summary>What a viewer may see. Pure, so the rules of each phase are easy to test.</summary>
    public static RetroView Build(Retro retro, IReadOnlyList<RetroCard> cards, IReadOnlyList<RetroVote> allVotes,
        IReadOnlyList<RetroActionItem> teamItems, Guid viewerId)
    {
        var counts = Enumerable.Range(0, retro.Columns).Select(c => cards.Count(card => card.Column == c)).ToList();
        var visible = retro.Phase == RetroPhase.Writing ? cards.Where(c => c.CreatedBy == viewerId) : cards;
        var resultsShown = retro.Phase == RetroPhase.Discussing;
        var totals = allVotes.GroupBy(v => v.CardId).ToDictionary(g => g.Key, g => g.Count());
        var mine = allVotes.Where(v => v.PlayerId == viewerId).GroupBy(v => v.CardId).ToDictionary(g => g.Key, g => g.Count());

        var items = visible
            .Select(c => new RetroCardItem(
                c.Id, c.Column, c.Text, c.CreatedBy == viewerId, retro.ShowAuthors ? c.CreatedBy : null,
                c.GroupId, c.GroupTitle, mine.GetValueOrDefault(c.Id), resultsShown ? totals.GetValueOrDefault(c.Id) : null))
            .ToList();
        if (resultsShown)
        {
            // Most votes first, cards of a group right after their head.
            var votesOfHead = items.Where(i => i.GroupId is null).ToDictionary(i => i.Id, i => i.Votes ?? 0);
            items = [.. items
                .OrderByDescending(i => votesOfHead.GetValueOrDefault(i.GroupId ?? i.Id))
                .ThenBy(i => i.GroupId is null ? 0 : 1)];
        }

        var used = allVotes.Count(v => v.PlayerId == viewerId);
        var own = teamItems.Where(i => i.RetroId == retro.Id).Select(ToResponse).ToList();
        // Open items agreed in earlier retros of the team come back here (OPP-166).
        var carried = teamItems.Where(i => i.RetroId != retro.Id && i.CreatedOn < retro.CreatedOn && (!i.Done || i.DoneOn >= retro.CreatedOn)).Select(ToResponse).ToList();

        return new RetroView(retro.Id, retro.TeamId, retro.GameId, retro.Template, retro.Columns, retro.Phase, retro.ShowAuthors,
            retro.DotsPerPerson, Math.Max(0, retro.DotsPerPerson - used), counts, items, own, carried);
    }

    public static RetroActionItemResponse ToResponse(RetroActionItem i) =>
        new(i.Id, i.RetroId, i.CardId, i.Text, i.OwnerId, i.Done, i.CreatedOn);

    public async Task<Result<Retro, ApplicationError>> UpdateSettingsAsync(Retro retro, bool showAuthors, int dots, CancellationToken cancellationToken)
    {
        if (!retro.UpdateSettings(showAuthors, dots)) return RetroErrors.WrongPhase;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return retro;
    }

    public async Task<Result<Retro, ApplicationError>> MoveToAsync(Retro retro, RetroPhase phase, CancellationToken cancellationToken)
    {
        if (!retro.MoveTo(phase)) return RetroErrors.WrongPhase;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return retro;
    }

    public async Task<Result<RetroCard, ApplicationError>> AddCardAsync(Retro retro, int column, string text, CancellationToken cancellationToken)
    {
        if (retro.Phase is not (RetroPhase.Writing or RetroPhase.Grouping)) return RetroErrors.WrongPhase;
        if (column < 0 || column >= retro.Columns) return RetroErrors.InvalidColumn;
        if (!ValidText(text, RetroCard.MaxTextLength)) return RetroErrors.InvalidText;
        var card = RetroCard.Create(retro.Id, column, text);
        retros.AddCard(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return card;
    }

    public async Task<Result<RetroCard, ApplicationError>> EditCardAsync(Retro retro, RetroCard card, Guid playerId, string text, CancellationToken cancellationToken)
    {
        if (card.CreatedBy != playerId) return RetroErrors.NotYourCard;
        if (retro.Phase is not (RetroPhase.Writing or RetroPhase.Grouping)) return RetroErrors.WrongPhase;
        if (!ValidText(text, RetroCard.MaxTextLength)) return RetroErrors.InvalidText;
        card.Edit(text);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return card;
    }

    /// <summary>The author, or the host (<paramref name="isHost"/>) to tidy the board. Cards of its group stay, on their own.</summary>
    public async Task<Result<bool, ApplicationError>> DeleteCardAsync(Retro retro, RetroCard card, Guid playerId, bool isHost, CancellationToken cancellationToken)
    {
        if (card.CreatedBy != playerId && !isHost) return RetroErrors.NotYourCard;
        if (retro.Phase is not (RetroPhase.Writing or RetroPhase.Grouping)) return RetroErrors.WrongPhase;
        foreach (var member in (await retros.GetCardsAsync(retro.Id, cancellationToken)).Where(c => c.GroupId == card.Id))
        {
            member.JoinGroup(null, member.Column);
        }
        retros.RemoveCard(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Drops <paramref name="card"/> onto <paramref name="targetId"/> (OPP-165): both end up in the target's group, and
    /// a group the card headed moves along. A null target takes the card out of its group.
    /// </summary>
    public async Task<Result<bool, ApplicationError>> GroupAsync(Retro retro, RetroCard card, Guid? targetId, CancellationToken cancellationToken)
    {
        if (retro.Phase != RetroPhase.Grouping) return RetroErrors.WrongPhase;
        var cards = await retros.GetCardsAsync(retro.Id, cancellationToken);
        if (targetId is null)
        {
            card.JoinGroup(null, card.Column);
        }
        else
        {
            var target = cards.FirstOrDefault(c => c.Id == targetId);
            if (target is null) return RetroErrors.CardNotFound;
            var headId = target.GroupId ?? target.Id;
            if (headId == card.Id) return true;
            var head = cards.First(c => c.Id == headId);
            foreach (var member in cards.Where(c => c.GroupId == card.Id))
            {
                member.JoinGroup(headId, head.Column);
            }
            card.JoinGroup(headId, head.Column);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Result<RetroCard, ApplicationError>> SetGroupTitleAsync(Retro retro, RetroCard head, string? title, CancellationToken cancellationToken)
    {
        if (retro.Phase is not (RetroPhase.Grouping or RetroPhase.Voting)) return RetroErrors.WrongPhase;
        if (title?.Trim().Length > RetroCard.MaxGroupTitleLength) return RetroErrors.InvalidText;
        head.SetGroupTitle(title);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return head;
    }

    /// <summary>Adds (<paramref name="add"/>) or takes back one dot on a card or group head.</summary>
    public async Task<Result<int, ApplicationError>> VoteAsync(Retro retro, RetroCard card, Guid playerId, bool add, CancellationToken cancellationToken)
    {
        if (retro.Phase != RetroPhase.Voting) return RetroErrors.WrongPhase;
        var targetId = card.GroupId ?? card.Id;
        var mine = (await retros.GetVotesAsync(retro.Id, cancellationToken)).Where(v => v.PlayerId == playerId).ToList();
        if (add)
        {
            if (mine.Count >= retro.DotsPerPerson) return RetroErrors.NoDotsLeft;
            retros.AddVote(RetroVote.Create(retro.Id, targetId, playerId));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return retro.DotsPerPerson - mine.Count - 1;
        }

        if (mine.FirstOrDefault(v => v.CardId == targetId) is { } vote)
        {
            retros.RemoveVote(vote);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return retro.DotsPerPerson - mine.Count + 1;
        }
        return retro.DotsPerPerson - mine.Count;
    }

    public async Task<Result<RetroActionItem, ApplicationError>> AddActionItemAsync(Retro retro, Guid? cardId, string text, Guid? ownerId, CancellationToken cancellationToken)
    {
        if (!ValidText(text, RetroActionItem.MaxTextLength)) return RetroErrors.InvalidText;
        if (cardId is { } id && (await retros.GetCardAsync(id, cancellationToken))?.RetroId != retro.Id) return RetroErrors.CardNotFound;
        if (!await IsPossibleOwnerAsync(retro, ownerId, cancellationToken)) return RetroErrors.InvalidOwner;
        var item = RetroActionItem.Create(retro.TeamId, retro.Id, cardId, text, ownerId);
        retros.AddActionItem(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<Result<RetroActionItem, ApplicationError>> UpdateActionItemAsync(RetroActionItem item, string text, Guid? ownerId, CancellationToken cancellationToken)
    {
        if (!ValidText(text, RetroActionItem.MaxTextLength)) return RetroErrors.InvalidText;
        var retro = await retros.GetAsync(item.RetroId, cancellationToken);
        if (retro is null || !await IsPossibleOwnerAsync(retro, ownerId, cancellationToken)) return RetroErrors.InvalidOwner;
        item.Update(text, ownerId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<RetroActionItem> SetActionItemDoneAsync(RetroActionItem item, bool done, CancellationToken cancellationToken)
    {
        item.SetDone(done);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task DeleteActionItemAsync(RetroActionItem item, CancellationToken cancellationToken)
    {
        retros.RemoveActionItem(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Open action items of the team, for the team room.</summary>
    public async Task<IReadOnlyList<RetroActionItemResponse>> OpenActionItemsAsync(Guid teamId, CancellationToken cancellationToken) =>
        [.. (await retros.GetActionItemsAsync(teamId, cancellationToken)).Where(i => !i.Done).Select(ToResponse)];

    /// <summary>Team members and the players of the sprint can own an action item.</summary>
    private async Task<bool> IsPossibleOwnerAsync(Retro retro, Guid? ownerId, CancellationToken cancellationToken)
    {
        if (ownerId is null) return true;
        if ((await teams.GetMembersAsync(retro.TeamId, cancellationToken)).Any(m => m.PlayerId == ownerId)) return true;
        return (await players.GetByGame(retro.GameId, cancellationToken)).Any(p => p.PlayerId == ownerId);
    }

    private static bool ValidText(string? text, int max) => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= max;

    /// <summary>
    /// "Sprint in numbers" (OPP-167) from data the app already has: no tracker is called, so cycle-time outliers come
    /// from what the team insights already read from Jira.
    /// </summary>
    public async Task<SprintFacts> FactsAsync(Retro retro, CancellationToken cancellationToken)
    {
        var sprints = (await sender.Send(new GetTeamSprintsQuery(retro.TeamId), cancellationToken))
            .OrderBy(s => s.CreatedOn).ToList();
        var current = sprints.FirstOrDefault(s => s.GameId == retro.GameId);
        var game = await games.GetByIdAsync(retro.GameId, cancellationToken);
        if (current is null || game is null || current.EstimatedCount == 0)
        {
            return new SprintFacts(false, 0, 0, current?.TicketCount ?? 0, null, [], [], []);
        }

        var index = sprints.IndexOf(current);
        var velocity = sprints.Skip(Math.Max(0, index - 4)).Take(Math.Min(index + 1, 5))
            .Select(s => new VelocityPoint(s.GameId, s.Name, s.Points, s.GameId == retro.GameId)).ToList();

        var gameTickets = (await tickets.GetByGame(retro.GameId, cancellationToken)).ToList();
        var planned = gameTickets.Where(t => t.Planned).ToList();
        double? plannedPoints = planned.Count == 0 ? null
            : planned.Sum(t => Planning.PointsOf(t.FinalEstimate) ?? 0);

        var gameVotes = await votes.GetByGame(retro.GameId, null, cancellationToken);
        var lowConfidence = gameTickets
            .Select(t => (ticket: t, confidence: gameVotes.Where(v => v.TicketId == t.Id && v.Confidence is not null).Select(v => (double)v.Confidence!.Value).ToList()))
            .Where(x => x.confidence.Count > 0 && x.confidence.Average() < 3)
            .OrderBy(x => x.confidence.Average())
            .Take(5)
            .Select(x => new SprintFactTicket(x.ticket.Id, x.ticket.Name, x.ticket.ExternalKey, x.ticket.FinalEstimate, null, null, Math.Round(x.confidence.Average(), 1)))
            .ToList();

        var outliers = await OutliersAsync(retro, gameTickets, cancellationToken);
        return new SprintFacts(true, current.Points, current.EstimatedCount, current.TicketCount, plannedPoints, velocity, outliers, lowConfidence);
    }

    /// <summary>Tickets of this sprint whose cycle time is far from the median of their card across the team.</summary>
    private async Task<IReadOnlyList<SprintFactTicket>> OutliersAsync(Retro retro, IReadOnlyList<Ticket> gameTickets, CancellationToken cancellationToken)
    {
        var teamTickets = await TeamReference.EstimatedTicketsAsync(retro.TeamId, games, tickets, votes, cancellationToken);
        var times = (await cycleTimes.GetAsync([.. teamTickets.Select(t => t.TicketId)], cancellationToken))
            .Where(c => c.Days is not null).ToDictionary(c => c.TicketId, c => c.Days!.Value);
        var medians = teamTickets.Where(t => times.ContainsKey(t.TicketId)).GroupBy(t => t.Estimate)
            .Where(g => g.Count() >= 3)
            .ToDictionary(g => g.Key, g => Median([.. g.Select(t => times[t.TicketId]).OrderBy(d => d)]));
        var ids = gameTickets.Select(t => t.Id).ToHashSet();
        return [.. teamTickets
            .Where(t => ids.Contains(t.TicketId) && times.ContainsKey(t.TicketId) && medians.TryGetValue(t.Estimate, out var m) && m > 0
                && (times[t.TicketId] > m * 2 || times[t.TicketId] < m / 2))
            .Select(t => new SprintFactTicket(t.TicketId, t.Name, t.ExternalKey, t.Estimate, times[t.TicketId], medians[t.Estimate], null))
            .OrderByDescending(f => Math.Abs(f.Days!.Value - f.CardMedianDays!.Value))
            .Take(5)];
    }

    private static double Median(IReadOnlyList<double> sorted) =>
        sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : Math.Round((sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2, 1);

    /// <summary>Column titles of a template in the request's language.</summary>
    public static IReadOnlyList<string> ColumnTitles(string template) =>
        [.. Enumerable.Range(0, Retro.Templates[template]).Select(i => ServerText.Get($"retro.{template}.{i}"))];

    /// <summary>The retro as Markdown (OPP-168): groups and cards by column with votes, then the action items.</summary>
    public static string Markdown(string sprintName, Retro retro, IReadOnlyList<RetroCard> cards, IReadOnlyList<RetroVote> allVotes,
        IReadOnlyList<RetroActionItem> items, Func<Guid, string?> nameOf)
    {
        var titles = ColumnTitles(retro.Template);
        var totals = allVotes.GroupBy(v => v.CardId).ToDictionary(g => g.Key, g => g.Count());
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# {ServerText.Get("retro.title")}: {sprintName}").AppendLine();
        for (var column = 0; column < titles.Count; column++)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"## {titles[column]}").AppendLine();
            var heads = cards.Where(c => c.Column == column && c.GroupId is null)
                .OrderByDescending(c => totals.GetValueOrDefault(c.Id)).ToList();
            if (heads.Count == 0) sb.AppendLine("—");
            foreach (var head in heads)
            {
                var members = cards.Where(c => c.GroupId == head.Id).ToList();
                var votesText = totals.GetValueOrDefault(head.Id) is > 0 and var n ? $" ({ServerText.Get("retro.votes", n)})" : string.Empty;
                if (members.Count == 0)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"- {head.Text}{votesText}");
                    continue;
                }
                sb.AppendLine(CultureInfo.InvariantCulture, $"- **{head.GroupTitle ?? head.Text}**{votesText}");
                foreach (var card in head.GroupTitle is null ? members : [head, .. members])
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  - {card.Text}");
                }
            }
            sb.AppendLine();
        }

        var own = items.Where(i => i.RetroId == retro.Id).ToList();
        sb.AppendLine(CultureInfo.InvariantCulture, $"## {ServerText.Get("retro.actionItems")}").AppendLine();
        if (own.Count == 0) sb.AppendLine("—");
        foreach (var item in own)
        {
            var owner = item.OwnerId is { } id && nameOf(id) is { } name ? $" — {name}" : string.Empty;
            sb.AppendLine(CultureInfo.InvariantCulture, $"- [{(item.Done ? "x" : " ")}] {item.Text}{owner}");
        }
        return sb.ToString();
    }

    /// <summary>Everything an export needs, with the names of the action item owners.</summary>
    public sealed record RetroExport(string SprintName, Retro Retro, IReadOnlyList<RetroCard> Cards, IReadOnlyList<RetroVote> Votes,
        IReadOnlyList<RetroActionItem> Items, IReadOnlyDictionary<Guid, string> Names)
    {
        public string? NameOf(Guid id) => Names.GetValueOrDefault(id);
    }

    public async Task<RetroExport> LoadExportAsync(Retro retro, CancellationToken cancellationToken)
    {
        var game = await games.GetByIdAsync(retro.GameId, cancellationToken);
        var items = await retros.GetActionItemsAsync(retro.TeamId, cancellationToken);
        var names = new Dictionary<Guid, string>();
        foreach (var owner in items.Where(i => i.RetroId == retro.Id && i.OwnerId is not null).Select(i => i.OwnerId!.Value).Distinct())
        {
            if ((await users.GetAsync($"{owner}", cancellationToken))?.UserName is { Length: > 0 } name) names[owner] = name;
        }
        return new RetroExport(game?.Name ?? string.Empty, retro, await retros.GetCardsAsync(retro.Id, cancellationToken),
            await retros.GetVotesAsync(retro.Id, cancellationToken), items, names);
    }

    public static string Markdown(RetroExport e) => Markdown(e.SprintName, e.Retro, e.Cards, e.Votes, e.Items, e.NameOf);

    /// <summary>Short text for the team's Slack or Teams webhook: the top topics and the action items.</summary>
    public static string Summary(string sprintName, Retro retro, IReadOnlyList<RetroCard> cards, IReadOnlyList<RetroVote> allVotes,
        IReadOnlyList<RetroActionItem> items, Func<Guid, string?> nameOf, string appUrl)
    {
        var totals = allVotes.GroupBy(v => v.CardId).ToDictionary(g => g.Key, g => g.Count());
        var top = cards.Where(c => c.GroupId is null && totals.GetValueOrDefault(c.Id) > 0)
            .OrderByDescending(c => totals[c.Id]).Take(3)
            .Select(c => $"{c.GroupTitle ?? c.Text} ({totals[c.Id]})").ToList();
        var own = items.Where(i => i.RetroId == retro.Id).ToList();
        var lines = new List<string> { ServerText.Get("retro.summary", sprintName, cards.Count, own.Count) };
        if (top.Count > 0) lines.Add(ServerText.Get("retro.summaryTop", string.Join("; ", top)));
        foreach (var item in own)
        {
            lines.Add(item.OwnerId is { } id && nameOf(id) is { } name ? $"• {item.Text} — {name}" : $"• {item.Text}");
        }
        lines.Add(appUrl);
        return string.Join("\n", lines);
    }

    public async Task<Result<bool, ApplicationError>> PostSummaryAsync(Retro retro, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(retro.TeamId, cancellationToken);
        if (team?.SummaryWebhookUrl is not { } url) return InsightsErrors.NoDestination;
        var e = await LoadExportAsync(retro, cancellationToken);
        var text = Summary(e.SprintName, retro, e.Cards, e.Votes, e.Items, e.NameOf, $"{InsightsService.AppUrl}/retro/{retro.GameId}");
        try
        {
            return await webhooks.PostAsync(url, text, cancellationToken) ? true : InsightsErrors.PostFailed;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return InsightsErrors.PostFailed;
        }
    }

    public Task<Team?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken) => teams.GetByIdAsync(teamId, cancellationToken);
}
