namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record DeleteTicketCommand(Guid TicketId) : IRequest<Result<bool, ApplicationError>>, IAuditedRequest
{
    public AuditEntry? ToAuditEntry() => AuditEntry.Ticket(AuditActions.TicketDeleted, AuditType.Delete, TicketId);
}

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
        : IRequestHandler<DeleteTicketCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(DeleteTicketCommand request, CancellationToken cancellationToken = default)
        {
            var ticket = (await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken))!;
            ticketRepository.Delete(ticket);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}