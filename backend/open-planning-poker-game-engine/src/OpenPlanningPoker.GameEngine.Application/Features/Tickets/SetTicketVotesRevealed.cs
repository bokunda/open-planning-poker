namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

/// <summary>
/// Reveals (true) or hides again (false, used by re-vote) the vote values of a ticket.
/// </summary>
public sealed record SetTicketVotesRevealedCommand(Guid TicketId, bool Revealed) : IRequest<Result<bool, ApplicationError>>;

public static class SetTicketVotesRevealed
{
    public class Validator : AbstractValidator<SetTicketVotesRevealedCommand>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
        }
    }

    public sealed class RequestHandler(ITicketRepository ticketRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<SetTicketVotesRevealedCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(SetTicketVotesRevealedCommand request, CancellationToken cancellationToken = default)
        {
            var ticket = await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            if (ticket.VotesRevealed == request.Revealed)
            {
                return true;
            }

            if (request.Revealed)
            {
                ticket.RevealVotes();
            }
            else
            {
                ticket.HideVotes();
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
