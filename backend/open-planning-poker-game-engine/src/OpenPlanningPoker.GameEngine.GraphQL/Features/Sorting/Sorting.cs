using OpenPlanningPoker.GameEngine.Application.Features.Sorting;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Sorting;

/// <summary>Place of one ticket on the sorting board (OPP-142).</summary>
public sealed class SortedTicket
{
    public Guid TicketId { get; set; }

    /// <summary>Card column; null while the ticket is unsorted.</summary>
    public string? Column { get; set; }

    /// <summary>Moves since the "discuss" mark was last cleared.</summary>
    public int Moves { get; set; }

    /// <summary>The team keeps moving the ticket, so it needs a talk (OPP-143).</summary>
    public bool Contested { get; set; }

    public static SortedTicket From(SortedTicketItem item) => new()
    {
        TicketId = item.TicketId,
        Column = item.Column,
        Moves = item.Moves,
        Contested = item.Contested,
    };
}

/// <summary>The sorting board of a game. Tickets that were never moved are not listed (they are unsorted).</summary>
public sealed class SortingBoardInfo
{
    public bool Open { get; set; }

    /// <summary>Card columns in deck order: every card except "?" and the break cards.</summary>
    public List<string> Columns { get; set; } = [];

    public List<SortedTicket> Tickets { get; set; } = [];

    public static SortingBoardInfo From(SortingBoardResponse r) => new()
    {
        Open = r.Open,
        Columns = [.. r.Columns],
        Tickets = [.. r.Tickets.Select(SortedTicket.From)],
    };
}

/// <summary>How many tickets got a final estimate from the board (OPP-144).</summary>
public sealed class SortedEstimatesApplied
{
    public int Applied { get; set; }

    /// <summary>Contested tickets, skipped until the team settles them.</summary>
    public int Contested { get; set; }

    /// <summary>Tickets still in the unsorted column.</summary>
    public int Unsorted { get; set; }
}

[QueryType]
public class SortingQueries
{
    /// <summary>The sorting board of a game. Players of the game only.</summary>
    public async Task<FieldResult<SortingBoardInfo, ApplicationError>> GetSortingBoardAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureMemberAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new GetSortingBoardQuery(gameId), cancellationToken);
        return result.IsSuccess ? SortingBoardInfo.From(result.Value!) : result.Error!;
    }
}

[MutationType]
public class SortingMutations
{
    /// <summary>Opens or closes the sorting board for everyone in the room. Host and co-hosts.</summary>
    public async Task<FieldResult<SortingBoardInfo, ApplicationError>> SetSortingOpenAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] bool open,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new SetSortingOpenCommand(gameId, open), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Sorting, cancellationToken: cancellationToken);
        return SortingBoardInfo.From(result.Value!);
    }

    /// <summary>Moves a ticket to a card column (an empty column moves it back to unsorted). Players, not observers.</summary>
    public async Task<FieldResult<SortedTicket, ApplicationError>> MoveSortedTicketAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        string? column,
        CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureVoterOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        if (rateLimiter.TryAcquire(ActionRateLimiter.SortMove, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        var result = await sender.Send(new MoveSortedTicketCommand(ticketId, column), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Sorting, ticketId: ticketId, cancellationToken: cancellationToken);
        return SortedTicket.From(result.Value!);
    }

    /// <summary>Clears the "discuss" mark of a ticket once the team agrees. Host and co-hosts.</summary>
    public async Task<FieldResult<SortedTicket, ApplicationError>> ClearSortContestedAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        var result = await sender.Send(new ClearSortContestedCommand(ticketId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Sorting, ticketId: ticketId, cancellationToken: cancellationToken);
        return SortedTicket.From(result.Value!);
    }

    /// <summary>
    /// Sets the final estimate of every sorted ticket without one to its column's card. Contested and unsorted tickets
    /// are left as they are. Host and co-hosts.
    /// </summary>
    public async Task<FieldResult<SortedEstimatesApplied, ApplicationError>> ApplySortedEstimatesAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new ApplySortedEstimatesCommand(gameId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, cancellationToken: cancellationToken);
        await eventSender.PublishAsync(gameId, GameChangeKind.Sorting, cancellationToken: cancellationToken);
        var r = result.Value!;
        return new SortedEstimatesApplied { Applied = r.Applied, Contested = r.Contested, Unsorted = r.Unsorted };
    }
}
