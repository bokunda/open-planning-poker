using OpenPlanningPoker.GameEngine.Domain.Teams;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Teams;

/// <summary>A card on the retro board as the caller may see it (OPP-163).</summary>
public sealed class RetroCardInfo
{
    public Guid Id { get; set; }
    public int Column { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>The caller wrote it.</summary>
    public bool Mine { get; set; }

    /// <summary>The author, only when the host shows names.</summary>
    public Guid? AuthorId { get; set; }

    /// <summary>The card this one was merged into (OPP-165); null for a card on its own or a group head.</summary>
    public Guid? GroupId { get; set; }
    public string? GroupTitle { get; set; }

    /// <summary>The caller's own dots on it.</summary>
    public int MyVotes { get; set; }

    /// <summary>All dots on it, once voting has ended; null before.</summary>
    public int? Votes { get; set; }

    public static RetroCardInfo From(RetroCardItem c) => new()
    {
        Id = c.Id, Column = c.Column, Text = c.Text, Mine = c.Mine, AuthorId = c.AuthorId,
        GroupId = c.GroupId, GroupTitle = c.GroupTitle, MyVotes = c.MyVotes, Votes = c.Votes,
    };
}

public sealed class RetroActionItemInfo
{
    public Guid Id { get; set; }
    public Guid RetroId { get; set; }
    public Guid? CardId { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public bool Done { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public static RetroActionItemInfo From(RetroActionItemResponse i) => new()
    {
        Id = i.Id, RetroId = i.RetroId, CardId = i.CardId, Text = i.Text, OwnerId = i.OwnerId, Done = i.Done, CreatedOn = i.CreatedOn,
    };
}

/// <summary>The retro of a sprint as the caller sees it (OPP-162).</summary>
public sealed class RetroInfo
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid GameId { get; set; }

    /// <summary>went-well, start-stop-continue, mad-sad-glad or 4ls; the app translates the column titles.</summary>
    public string Template { get; set; } = string.Empty;
    public int Columns { get; set; }
    public RetroPhase Phase { get; set; }
    public bool ShowAuthors { get; set; }
    public int DotsPerPerson { get; set; }
    public int DotsLeft { get; set; }

    /// <summary>Cards per column, everyone's; while writing the only thing others see.</summary>
    public List<int> CountPerColumn { get; set; } = [];
    public List<RetroCardInfo> Cards { get; set; } = [];

    /// <summary>Action items agreed in this retro.</summary>
    public List<RetroActionItemInfo> ActionItems { get; set; } = [];

    /// <summary>Open items of earlier retros of the team, and the ones closed during this retro.</summary>
    public List<RetroActionItemInfo> CarriedOver { get; set; } = [];

    /// <summary>The caller runs the retro: host or co-host of the sprint, or the team owner.</summary>
    public bool CanFacilitate { get; set; }

    public static RetroInfo From(RetroView v, bool canFacilitate) => new()
    {
        Id = v.Id, TeamId = v.TeamId, GameId = v.GameId, Template = v.Template, Columns = v.Columns, Phase = v.Phase,
        ShowAuthors = v.ShowAuthors, DotsPerPerson = v.DotsPerPerson, DotsLeft = v.DotsLeft,
        CountPerColumn = [.. v.CountPerColumn], Cards = [.. v.Cards.Select(RetroCardInfo.From)],
        ActionItems = [.. v.ActionItems.Select(RetroActionItemInfo.From)], CarriedOver = [.. v.CarriedOver.Select(RetroActionItemInfo.From)],
        CanFacilitate = canFacilitate,
    };
}

/// <summary>Where a sprint's retro stands, for the team room.</summary>
public sealed class SprintRetro
{
    public Guid GameId { get; set; }
    public RetroPhase Phase { get; set; }
}

public sealed class SprintFactsInfo
{
    public bool HasData { get; set; }
    public double EstimatedPoints { get; set; }
    public int EstimatedTickets { get; set; }
    public int TicketCount { get; set; }
    public double? PlannedPoints { get; set; }
    public List<VelocityPoint> Velocity { get; set; } = [];
    public List<SprintFactTicket> Outliers { get; set; } = [];
    public List<SprintFactTicket> LowConfidence { get; set; } = [];

    public static SprintFactsInfo From(SprintFacts f) => new()
    {
        HasData = f.HasData, EstimatedPoints = f.EstimatedPoints, EstimatedTickets = f.EstimatedTickets, TicketCount = f.TicketCount,
        PlannedPoints = f.PlannedPoints, Velocity = [.. f.Velocity], Outliers = [.. f.Outliers], LowConfidence = [.. f.LowConfidence],
    };
}

public sealed class RetroExportFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
}

/// <summary>Who may do what in a retro: everyone in the sprint's room takes part; the host or the team owner runs it.</summary>
internal static class RetroAccess
{
    public static async Task<bool> CanFacilitateAsync(IGameAccessService access, Guid teamId, Guid gameId, CancellationToken cancellationToken) =>
        await access.EnsureHostAsync(gameId, cancellationToken) is null || await access.EnsureTeamOwnerAsync(teamId, cancellationToken) is null;

    /// <summary>The retro of a sprint the caller joined.</summary>
    public static async Task<(Retro? Retro, ApplicationError? Error)> ParticipantAsync(
        RetroService retros, IGameAccessService access, Guid retroId, CancellationToken cancellationToken)
    {
        var retro = await retros.GetAsync(retroId, cancellationToken);
        if (retro is null) return (null, RetroErrors.NotFound);
        return await access.EnsureMemberAsync(retro.GameId, cancellationToken) is { } error ? (null, error) : (retro, null);
    }

    public static async Task<(Retro? Retro, ApplicationError? Error)> FacilitatorAsync(
        RetroService retros, IGameAccessService access, Guid retroId, CancellationToken cancellationToken)
    {
        var retro = await retros.GetAsync(retroId, cancellationToken);
        if (retro is null) return (null, RetroErrors.NotFound);
        if (await access.EnsureMemberAsync(retro.GameId, cancellationToken) is { } error) return (null, error);
        return await CanFacilitateAsync(access, retro.TeamId, retro.GameId, cancellationToken)
            ? (retro, null)
            : (null, new ApplicationError("NotHost", "Only the game host can perform this action."));
    }

    public static async Task<RetroInfo> InfoAsync(RetroService retros, IGameAccessService access, Retro retro, CancellationToken cancellationToken) =>
        RetroInfo.From(await retros.ViewAsync(retro, access.CurrentUserId, cancellationToken),
            await CanFacilitateAsync(access, retro.TeamId, retro.GameId, cancellationToken));
}

[ExtendObjectType(typeof(Team))]
public sealed class TeamRetroFields
{
    /// <summary>Sprints that have a retro and its step. Empty for non-members.</summary>
    public async Task<List<SprintRetro>> GetRetrosAsync(
        [Parent] Team team, [Service] IGameAccessService gameAccess, [Service] RetroService retros, CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureTeamMemberAsync(team.Id, cancellationToken) is not null) return [];
        return [.. (await retros.GetByTeamAsync(team.Id, cancellationToken)).Values.Select(r => new SprintRetro { GameId = r.GameId, Phase = r.Phase })];
    }

    /// <summary>Open action items of the team's retros (OPP-166). Empty for non-members.</summary>
    public async Task<List<RetroActionItemInfo>> GetOpenActionItemsAsync(
        [Parent] Team team, [Service] IGameAccessService gameAccess, [Service] RetroService retros, CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureTeamMemberAsync(team.Id, cancellationToken) is not null) return [];
        return [.. (await retros.OpenActionItemsAsync(team.Id, cancellationToken)).Select(RetroActionItemInfo.From)];
    }
}

[QueryType]
public class RetroQueries
{
    /// <summary>The retro of a sprint (game). Players of the game only; RetroNotFound when it has none yet.</summary>
    public async Task<FieldResult<RetroInfo, ApplicationError>> GetRetroAsync(
        [Service] RetroService retros,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureMemberAsync(gameId, cancellationToken) is { } accessError) return accessError;
        var retro = await retros.GetByGameAsync(gameId, cancellationToken);
        if (retro is null) return RetroErrors.NotFound;
        return await RetroAccess.InfoAsync(retros, gameAccess, retro, cancellationToken);
    }

    /// <summary>"Sprint in numbers" for the retro (OPP-167). Players of the game.</summary>
    public async Task<FieldResult<SprintFactsInfo, ApplicationError>> GetRetroFactsAsync(
        [Service] RetroService retros,
        [Service] IGameAccessService gameAccess,
        [Required] Guid retroId,
        CancellationToken cancellationToken = default)
    {
        var (retro, error) = await RetroAccess.ParticipantAsync(retros, gameAccess, retroId, cancellationToken);
        if (error is not null) return error;
        return SprintFactsInfo.From(await retros.FactsAsync(retro!, cancellationToken));
    }

    /// <summary>The retro as Markdown or PDF (OPP-168). Players of the game.</summary>
    public async Task<FieldResult<RetroExportFile, ApplicationError>> GetRetroExportAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] RetroService retros,
        [Service] IGameAccessService gameAccess,
        [Required] Guid retroId,
        bool pdf = false,
        CancellationToken cancellationToken = default)
    {
        var (retro, error) = await RetroAccess.ParticipantAsync(retros, gameAccess, retroId, cancellationToken);
        if (error is not null) return error;
        if (rateLimiter.TryAcquire(ActionRateLimiter.GenerateReport, gameAccess.CurrentUserId) is { } rateLimitError) return rateLimitError;

        var export = await retros.LoadExportAsync(retro!, cancellationToken);
        var baseName = string.Concat(export.SprintName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')).Trim('-');
        if (baseName.Length == 0) baseName = "retro";
        return pdf
            ? new RetroExportFile { FileName = $"{baseName}-retro.pdf", ContentType = "application/pdf", Data = RetroDocument.Render(export) }
            : new RetroExportFile
            {
                FileName = $"{baseName}-retro.md", ContentType = "text/markdown",
                Data = System.Text.Encoding.UTF8.GetBytes(RetroService.Markdown(export)),
            };
    }
}

[MutationType]
public class RetroMutations
{
    /// <summary>Starts the retro of a team sprint from a template (OPP-163). Team owner or the sprint's host.</summary>
    public async Task<FieldResult<RetroInfo, ApplicationError>> StartRetroAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        [Required] Guid gameId,
        [Required] string template,
        CancellationToken cancellationToken = default)
    {
        if (!await RetroAccess.CanFacilitateAsync(gameAccess, teamId, gameId, cancellationToken))
        {
            return new ApplicationError("NotTeamOwner", "Only the team owner can perform this action.");
        }

        var result = await retros.StartAsync(teamId, gameId, template, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(gameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return RetroInfo.From(await retros.ViewAsync(result.Value!, gameAccess.CurrentUserId, cancellationToken), true);
    }

    /// <summary>Names on cards and dots per person; only while writing (OPP-164, OPP-165). Host.</summary>
    public async Task<FieldResult<RetroInfo, ApplicationError>> UpdateRetroSettingsAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid retroId,
        [Required] bool showAuthors,
        [Required] int dotsPerPerson,
        CancellationToken cancellationToken = default)
    {
        var (retro, error) = await RetroAccess.FacilitatorAsync(retros, gameAccess, retroId, cancellationToken);
        if (error is not null) return error;
        var result = await retros.UpdateSettingsAsync(retro!, showAuthors, dotsPerPerson, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(retro!.GameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return await RetroAccess.InfoAsync(retros, gameAccess, retro, cancellationToken);
    }

    /// <summary>Moves the retro to the next step: reveal, voting, results (OPP-164, OPP-165). Host.</summary>
    public async Task<FieldResult<RetroInfo, ApplicationError>> SetRetroPhaseAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid retroId,
        [Required] RetroPhase phase,
        CancellationToken cancellationToken = default)
    {
        var (retro, error) = await RetroAccess.FacilitatorAsync(retros, gameAccess, retroId, cancellationToken);
        if (error is not null) return error;
        var result = await retros.MoveToAsync(retro!, phase, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(retro!.GameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return await RetroAccess.InfoAsync(retros, gameAccess, retro, cancellationToken);
    }

    /// <summary>Writes a card in a column. Players of the sprint.</summary>
    public async Task<FieldResult<RetroCardInfo, ApplicationError>> AddRetroCardAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid retroId,
        [Required] int column,
        [Required] string text,
        CancellationToken cancellationToken = default)
    {
        var (retro, error) = await RetroAccess.ParticipantAsync(retros, gameAccess, retroId, cancellationToken);
        if (error is not null) return error;
        if (rateLimiter.TryAcquire(ActionRateLimiter.RetroCard, gameAccess.CurrentUserId) is { } rateLimitError) return rateLimitError;
        var result = await retros.AddCardAsync(retro!, column, text, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(retro!.GameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        var c = result.Value!;
        return new RetroCardInfo { Id = c.Id, Column = c.Column, Text = c.Text, Mine = true, AuthorId = retro.ShowAuthors ? gameAccess.CurrentUserId : null };
    }

    /// <summary>Changes the text of one's own card.</summary>
    public async Task<FieldResult<bool, ApplicationError>> EditRetroCardAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid cardId,
        [Required] string text,
        CancellationToken cancellationToken = default)
    {
        var (retro, card, error) = await CardAsync(retros, gameAccess, cardId, cancellationToken);
        if (error is not null) return error;
        var result = await retros.EditCardAsync(retro!, card!, gameAccess.CurrentUserId, text, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(retro!.GameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>Deletes a card: its author, or the host.</summary>
    public async Task<FieldResult<bool, ApplicationError>> DeleteRetroCardAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var (retro, card, error) = await CardAsync(retros, gameAccess, cardId, cancellationToken);
        if (error is not null) return error;
        var isHost = await RetroAccess.CanFacilitateAsync(gameAccess, retro!.TeamId, retro.GameId, cancellationToken);
        var result = await retros.DeleteCardAsync(retro, card!, gameAccess.CurrentUserId, isHost, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(retro.GameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>Drops a card onto another to group them; without a target it leaves its group (OPP-165). Players.</summary>
    public async Task<FieldResult<bool, ApplicationError>> GroupRetroCardAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid cardId,
        Guid? targetCardId,
        CancellationToken cancellationToken = default)
    {
        var (retro, card, error) = await CardAsync(retros, gameAccess, cardId, cancellationToken);
        if (error is not null) return error;
        if (rateLimiter.TryAcquire(ActionRateLimiter.RetroCard, gameAccess.CurrentUserId) is { } rateLimitError) return rateLimitError;
        var result = await retros.GroupAsync(retro!, card!, targetCardId, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(retro!.GameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>Names a group (the card that heads it). Players.</summary>
    public async Task<FieldResult<bool, ApplicationError>> SetRetroGroupTitleAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid cardId,
        string? title,
        CancellationToken cancellationToken = default)
    {
        var (retro, card, error) = await CardAsync(retros, gameAccess, cardId, cancellationToken);
        if (error is not null) return error;
        var result = await retros.SetGroupTitleAsync(retro!, card!, title, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(retro!.GameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>Puts one dot on a card (or takes one back). Returns the dots left. Dots stay hidden until the results.</summary>
    public async Task<FieldResult<int, ApplicationError>> VoteRetroCardAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] RetroService retros,
        [Service] IGameAccessService gameAccess,
        [Required] Guid cardId,
        [Required] bool add,
        CancellationToken cancellationToken = default)
    {
        var (retro, card, error) = await CardAsync(retros, gameAccess, cardId, cancellationToken);
        if (error is not null) return error;
        if (rateLimiter.TryAcquire(ActionRateLimiter.RetroCard, gameAccess.CurrentUserId) is { } rateLimitError) return rateLimitError;
        // No event: nobody else may see the dots before the results.
        var result = await retros.VoteAsync(retro!, card!, gameAccess.CurrentUserId, add, cancellationToken);
        return result.IsSuccess ? result.Value : result.Error!;
    }

    /// <summary>Adds an action item, optionally for a card and with an owner (OPP-166). Players.</summary>
    public async Task<FieldResult<RetroActionItemInfo, ApplicationError>> AddRetroActionItemAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid retroId,
        [Required] string text,
        Guid? cardId,
        Guid? ownerId,
        CancellationToken cancellationToken = default)
    {
        var (retro, error) = await RetroAccess.ParticipantAsync(retros, gameAccess, retroId, cancellationToken);
        if (error is not null) return error;
        var result = await retros.AddActionItemAsync(retro!, cardId, text, ownerId, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(retro!.GameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return RetroActionItemInfo.From(RetroService.ToResponse(result.Value!));
    }

    /// <summary>Changes the text or owner of an action item. Players of the team's sprint, or team members.</summary>
    public async Task<FieldResult<RetroActionItemInfo, ApplicationError>> UpdateRetroActionItemAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid actionItemId,
        [Required] string text,
        Guid? ownerId,
        CancellationToken cancellationToken = default)
    {
        var (item, gameId, error) = await ActionItemAsync(retros, gameAccess, actionItemId, cancellationToken);
        if (error is not null) return error;
        var result = await retros.UpdateActionItemAsync(item!, text, ownerId, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        await eventSender.PublishAsync(gameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return RetroActionItemInfo.From(RetroService.ToResponse(result.Value!));
    }

    /// <summary>Marks an action item done or open again. Team members, from the retro or the team room.</summary>
    public async Task<FieldResult<RetroActionItemInfo, ApplicationError>> SetRetroActionItemDoneAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid actionItemId,
        [Required] bool done,
        CancellationToken cancellationToken = default)
    {
        var (item, gameId, error) = await ActionItemAsync(retros, gameAccess, actionItemId, cancellationToken);
        if (error is not null) return error;
        var result = await retros.SetActionItemDoneAsync(item!, done, cancellationToken);
        await eventSender.PublishAsync(gameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return RetroActionItemInfo.From(RetroService.ToResponse(result));
    }

    public async Task<FieldResult<bool, ApplicationError>> DeleteRetroActionItemAsync(
        [Service] RetroService retros,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid actionItemId,
        CancellationToken cancellationToken = default)
    {
        var (item, gameId, error) = await ActionItemAsync(retros, gameAccess, actionItemId, cancellationToken);
        if (error is not null) return error;
        await retros.DeleteActionItemAsync(item!, cancellationToken);
        await eventSender.PublishAsync(gameId, GameChangeKind.Retro, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>Posts a short summary to the team's Slack or Teams webhook (OPP-168). Host.</summary>
    public async Task<FieldResult<bool, ApplicationError>> PostRetroSummaryAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] RetroService retros,
        [Service] IGameAccessService gameAccess,
        [Required] Guid retroId,
        CancellationToken cancellationToken = default)
    {
        var (retro, error) = await RetroAccess.FacilitatorAsync(retros, gameAccess, retroId, cancellationToken);
        if (error is not null) return error;
        if (rateLimiter.TryAcquire(ActionRateLimiter.GenerateReport, gameAccess.CurrentUserId) is { } rateLimitError) return rateLimitError;
        var result = await retros.PostSummaryAsync(retro!, cancellationToken);
        return result.IsSuccess ? true : result.Error!;
    }

    private static async Task<(Retro? Retro, RetroCard? Card, ApplicationError? Error)> CardAsync(
        RetroService retros, IGameAccessService access, Guid cardId, CancellationToken cancellationToken)
    {
        var card = await retros.GetCardAsync(cardId, cancellationToken);
        if (card is null) return (null, null, RetroErrors.CardNotFound);
        var (retro, error) = await RetroAccess.ParticipantAsync(retros, access, card.RetroId, cancellationToken);
        return (retro, card, error);
    }

    /// <summary>
    /// Action items live on after their retro: players of that sprint and the team's members may change them (the
    /// team room shows them too).
    /// </summary>
    private static async Task<(RetroActionItem? Item, Guid GameId, ApplicationError? Error)> ActionItemAsync(
        RetroService retros, IGameAccessService access, Guid actionItemId, CancellationToken cancellationToken)
    {
        var item = await retros.GetActionItemAsync(actionItemId, cancellationToken);
        if (item is null) return (null, Guid.Empty, RetroErrors.ActionItemNotFound);
        var retro = await retros.GetAsync(item.RetroId, cancellationToken);
        if (retro is null) return (null, Guid.Empty, RetroErrors.ActionItemNotFound);
        if (await access.EnsureMemberAsync(retro.GameId, cancellationToken) is not null
            && await access.EnsureTeamMemberAsync(item.TeamId, cancellationToken) is { } error)
        {
            return (null, Guid.Empty, error);
        }
        return (item, retro.GameId, null);
    }
}
