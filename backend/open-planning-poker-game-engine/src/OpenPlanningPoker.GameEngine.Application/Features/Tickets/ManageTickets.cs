namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

/// <summary>Puts the game's tickets in the given order. Tickets missing from the list keep their place after the listed ones.</summary>
public sealed record ReorderTicketsCommand(Guid GameId, IReadOnlyList<Guid> TicketIds) : IRequest<Result<bool, ApplicationError>>;

/// <summary>Stores the estimate the host settled on; a null or empty value clears it.</summary>
public sealed record SetFinalEstimateCommand(Guid TicketId, string? Value) : IRequest<Result<GetTicketResponse, ApplicationError>>;

public static class ManageTickets
{
    public const int MaxTicketsPerReorder = 500;

    public sealed class ReorderValidator : AbstractValidator<ReorderTicketsCommand>
    {
        public ReorderValidator()
        {
            RuleFor(x => x.GameId).NotEmpty();
            RuleFor(x => x.TicketIds)
                .NotEmpty()
                .Must(ids => ids.Count <= MaxTicketsPerReorder).WithMessage($"At most {MaxTicketsPerReorder} tickets can be reordered at once.")
                .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Each ticket can appear only once.");
        }
    }

    public sealed class FinalEstimateValidator : AbstractValidator<SetFinalEstimateCommand>
    {
        public FinalEstimateValidator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.Value).MaximumLength(Ticket.MaxFinalEstimateLength);
        }
    }

    public sealed class ReorderHandler(ITicketRepository tickets, IUnitOfWork unitOfWork)
        : IRequestHandler<ReorderTicketsCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(ReorderTicketsCommand request, CancellationToken cancellationToken)
        {
            var current = (await tickets.GetByGame(request.GameId, cancellationToken)).ToList();
            var byId = current.ToDictionary(ticket => ticket.Id);
            if (request.TicketIds.Any(id => !byId.ContainsKey(id)))
            {
                return GameAccessErrors.TicketNotFound;
            }

            var listed = request.TicketIds.Select(id => byId[id]);
            var rest = current.Where(ticket => !request.TicketIds.Contains(ticket.Id));
            var position = 0;
            foreach (var ticket in listed.Concat(rest))
            {
                ticket.MoveTo(position++);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public sealed class FinalEstimateHandler(ITicketRepository tickets, IUnitOfWork unitOfWork)
        : IRequestHandler<SetFinalEstimateCommand, Result<GetTicketResponse, ApplicationError>>
    {
        public async Task<Result<GetTicketResponse, ApplicationError>> Handle(SetFinalEstimateCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            if (!ticket.VotesRevealed && !string.IsNullOrWhiteSpace(request.Value))
            {
                return new ApplicationError("VotesNotRevealed", "Reveal the cards before choosing the final estimate.");
            }

            ticket.SetFinalEstimate(request.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ticket.ToGetTicketResponse();
        }
    }
}
