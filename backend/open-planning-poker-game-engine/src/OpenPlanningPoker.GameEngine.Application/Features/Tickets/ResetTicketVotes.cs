namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

/// <summary>
/// Starts a new voting round on a ticket: deletes all of its votes and hides votes again.
/// </summary>
public sealed record ResetTicketVotesCommand(Guid TicketId) : IRequest<Result<bool, ApplicationError>>;

public static class ResetTicketVotes
{
    public class Validator : AbstractValidator<ResetTicketVotesCommand>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
        }
    }

    public sealed class RequestHandler(
        ITicketRepository ticketRepository,
        IVoteRepository voteRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ResetTicketVotesCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(ResetTicketVotesCommand request, CancellationToken cancellationToken = default)
        {
            var ticket = await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            var votes = await voteRepository.GetByTicket(request.TicketId, null, cancellationToken);
            voteRepository.DeleteRange(votes);
            ticket.HideVotes();

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
