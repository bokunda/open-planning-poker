namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record CreateTicketResponse(Guid Id, Guid GameId, string Name, string Description, bool VotesRevealed, string? FinalEstimate = null, int Position = 0);

public sealed record CreateTicketCommand(Guid GameId, string Name, string Description) : IRequest<Result<CreateTicketResponse, ApplicationError>>;

public static class CreateTicket
{
    public class Validator : AbstractValidator<CreateTicketCommand>
    {
        public Validator()
        {
            RuleFor(x => x.GameId)
                .NotEmpty();

            RuleFor(x => x.Name)
                .MaximumLength(255)
                .NotEmpty();

            RuleFor(x => x.Description)
                .MaximumLength(4080);
        }
    }


    public sealed class RequestHandler(ITicketRepository ticketRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateTicketCommand, Result<CreateTicketResponse, ApplicationError>>
    {
        public async Task<Result<CreateTicketResponse, ApplicationError>> Handle(CreateTicketCommand request, CancellationToken cancellationToken = default)
        {
            var existing = await ticketRepository.GetByGame(request.GameId, cancellationToken);
            var position = existing.Select(item => item.Position).DefaultIfEmpty(-1).Max() + 1;
            var ticket = Ticket.Create(request.GameId, request.Name, request.Description, position);
            ticketRepository.Add(ticket);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ticket.ToCreateTicketResponse();
        }
    }
}