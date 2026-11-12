namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record ImportTicketItemResponse(string Name, string Description);
public sealed record ImportTicketItem(string Name, string Description);

public sealed record ImportTicketsCommand(Guid GameId, ICollection<ImportTicketItem> Tickets) : IRequest<Result<ApiCollection<ImportTicketItemResponse>, ApplicationError>>;

public static class ImportTickets
{
    public class Validator : AbstractValidator<ImportTicketsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Tickets)
                .NotEmpty();
        }
    }


    public sealed class RequestHandler(ITicketRepository ticketRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<ImportTicketsCommand, Result<ApiCollection<ImportTicketItemResponse>, ApplicationError>>
    {
        public async Task<Result<ApiCollection<ImportTicketItemResponse>, ApplicationError>> Handle(ImportTicketsCommand request, CancellationToken cancellationToken = default)
        {
            var existing = await ticketRepository.GetByGame(request.GameId, cancellationToken);
            var start = existing.Select(item => item.Position).DefaultIfEmpty(-1).Max() + 1;
            var tickets = request.Tickets
                .Select((ticket, index) => Ticket.Create(request.GameId, ticket.Name, ticket.Description, start + index))
                .ToList();
            ticketRepository.AddRange(tickets);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var mappedTickets = tickets.Select(item => item.ToImportTicketItemResponse()).ToList();
            return new ApiCollection<ImportTicketItemResponse>(mappedTickets, mappedTickets.Count);
        }
    }
}