namespace OpenPlanningPoker.GameEngine.Application.Features.Sorting;

/// <summary>Place of one ticket on the sorting board (OPP-142).</summary>
public sealed record SortedTicketItem(Guid TicketId, string? Column, int Moves, bool Contested);

/// <summary>The sorting board of a game: whether it is open, its card columns and where the tickets are.</summary>
public sealed record SortingBoardResponse(bool Open, IReadOnlyList<string> Columns, IReadOnlyList<SortedTicketItem> Tickets);

/// <summary>Result of turning the board into final estimates (OPP-144).</summary>
public sealed record ApplySortedEstimatesResponse(int Applied, int Contested, int Unsorted);

public sealed record GetSortingBoardQuery(Guid GameId) : IRequest<Result<SortingBoardResponse, ApplicationError>>;
public sealed record SetSortingOpenCommand(Guid GameId, bool Open) : IRequest<Result<SortingBoardResponse, ApplicationError>>;
public sealed record MoveSortedTicketCommand(Guid TicketId, string? Column) : IRequest<Result<SortedTicketItem, ApplicationError>>;
public sealed record ClearSortContestedCommand(Guid TicketId) : IRequest<Result<SortedTicketItem, ApplicationError>>;
public sealed record ApplySortedEstimatesCommand(Guid GameId) : IRequest<Result<ApplySortedEstimatesResponse, ApplicationError>>, IAuditedRequest
{
    public AuditEntry? ToAuditEntry() => AuditEntry.Game(AuditActions.SortingApplied, AuditType.Update, GameId);
}

public static class SortingErrors
{
    public static ApplicationError NotOpen => new("SortingClosed", "The sorting board is closed.");
    public static ApplicationError UnknownColumn => new("UnknownColumn", "Choose one of the game's cards.");
    public static ApplicationError AlreadyEstimated => new("AlreadyEstimated", "This ticket already has a final estimate.");
    public static ApplicationError NoSettings => new("GameSettingsNotFound", "Game settings not found.");
}

public static class SortingBoard
{
    /// <summary>
    /// Card columns of a deck: every card except "?" / "❓" and the break cards, which say nothing about size.
    /// </summary>
    public static IReadOnlyList<string> Columns(string deckSetup) =>
        [.. deckSetup.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(card => !card.Contains('?') && !card.Contains('❓') && !card.Contains('☕') && !card.Contains('⏸'))
            .Distinct()];

    public static SortedTicketItem ToItem(Ticket ticket) =>
        new(ticket.Id, ticket.SortColumn, ticket.SortMoves, ticket.SortContested);

    private static SortingBoardResponse Build(Domain.GameSettings.GameSettings settings, IEnumerable<Ticket> tickets) =>
        new(settings.SortingOpen, Columns(settings.DeckSetup),
            [.. tickets.Where(t => t.SortColumn is not null || t.SortContested || t.SortMoves > 0).Select(ToItem)]);

    public sealed class GetHandler(IGameSettingsRepository settingsRepository, ITicketRepository tickets)
        : IRequestHandler<GetSortingBoardQuery, Result<SortingBoardResponse, ApplicationError>>
    {
        public async Task<Result<SortingBoardResponse, ApplicationError>> Handle(GetSortingBoardQuery request, CancellationToken cancellationToken)
        {
            var settings = await settingsRepository.GetByGame(request.GameId, cancellationToken);
            return Build(settings, await tickets.GetByGame(request.GameId, cancellationToken));
        }
    }

    public sealed class OpenHandler(IGameSettingsRepository settingsRepository, ITicketRepository tickets, IUnitOfWork unitOfWork)
        : IRequestHandler<SetSortingOpenCommand, Result<SortingBoardResponse, ApplicationError>>
    {
        public async Task<Result<SortingBoardResponse, ApplicationError>> Handle(SetSortingOpenCommand request, CancellationToken cancellationToken)
        {
            var settings = await settingsRepository.GetByGame(request.GameId, cancellationToken);
            settings.SetSortingOpen(request.Open);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Build(settings, await tickets.GetByGame(request.GameId, cancellationToken));
        }
    }

    public sealed class MoveValidator : AbstractValidator<MoveSortedTicketCommand>
    {
        public MoveValidator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.Column).MaximumLength(Ticket.MaxFinalEstimateLength);
        }
    }

    public sealed class MoveHandler(ITicketRepository tickets, IGameSettingsRepository settingsRepository, ICurrentUserProvider currentUser, IUnitOfWork unitOfWork)
        : IRequestHandler<MoveSortedTicketCommand, Result<SortedTicketItem, ApplicationError>>
    {
        public async Task<Result<SortedTicketItem, ApplicationError>> Handle(MoveSortedTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            var settings = await settingsRepository.GetByGame(ticket.GameId, cancellationToken);
            if (!settings.SortingOpen)
            {
                return SortingErrors.NotOpen;
            }

            if (ticket.FinalEstimate is not null)
            {
                return SortingErrors.AlreadyEstimated;
            }

            var column = string.IsNullOrWhiteSpace(request.Column) ? null : request.Column.Trim();
            if (column is not null && !Columns(settings.DeckSetup).Contains(column))
            {
                return SortingErrors.UnknownColumn;
            }

            if (ticket.MoveOnBoard(column, currentUser.Id))
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return ToItem(ticket);
        }
    }

    public sealed class ClearHandler(ITicketRepository tickets, IUnitOfWork unitOfWork)
        : IRequestHandler<ClearSortContestedCommand, Result<SortedTicketItem, ApplicationError>>
    {
        public async Task<Result<SortedTicketItem, ApplicationError>> Handle(ClearSortContestedCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            ticket.ClearContested();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ToItem(ticket);
        }
    }

    public sealed class ApplyHandler(ITicketRepository tickets, IUnitOfWork unitOfWork)
        : IRequestHandler<ApplySortedEstimatesCommand, Result<ApplySortedEstimatesResponse, ApplicationError>>
    {
        public async Task<Result<ApplySortedEstimatesResponse, ApplicationError>> Handle(ApplySortedEstimatesCommand request, CancellationToken cancellationToken)
        {
            var open = (await tickets.GetByGame(request.GameId, cancellationToken)).Where(t => t.FinalEstimate is null).ToList();
            var applied = open.Count(ticket => ticket.ApplySortedEstimate());
            if (applied > 0)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new ApplySortedEstimatesResponse(applied, open.Count(t => t.SortContested), open.Count(t => t.SortColumn is null && !t.SortContested));
        }
    }
}
