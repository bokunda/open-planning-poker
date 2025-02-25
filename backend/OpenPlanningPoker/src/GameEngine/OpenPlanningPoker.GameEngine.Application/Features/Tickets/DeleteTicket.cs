namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record DeleteTicketResponse;

public sealed record DeleteTicketCommand(Guid TicketId) : IRequest<DeleteTicketResponse>;

public static class DeleteTicket
{
    public class Validator : AbstractValidator<DeleteTicketCommand>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty();
        }
    }

    public sealed class RequestHandler(ITicketRepository ticketRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteTicketCommand, DeleteTicketResponse>
    {
        public async Task<DeleteTicketResponse> Handle(DeleteTicketCommand request, CancellationToken cancellationToken = default)
        {
            var ticket = (await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken))!;
            ticketRepository.Delete(ticket);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteTicketResponse();
        }
    }
}