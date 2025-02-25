namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record ImportTicketItemResponse(string Name, string Description);
public sealed record ImportTicketItem(string Name, string Description);

public sealed record ImportTicketsCommand(Guid GameId, ICollection<ImportTicketItem> Tickets) : IRequest<ApiCollection<ImportTicketItemResponse>>;

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

    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ImportTicketsCommand, Ticket>();
            CreateMap<Ticket, ImportTicketItemResponse>();
        }
    }

    public sealed class RequestHandler(ITicketRepository ticketRepository, IMapper mapper, IUnitOfWork unitOfWork)
        : IRequestHandler<ImportTicketsCommand, ApiCollection<ImportTicketItemResponse>>
    {
        public async Task<ApiCollection<ImportTicketItemResponse>> Handle(ImportTicketsCommand request, CancellationToken cancellationToken = default)
        {
            var tickets = request.Tickets.Select(ticket => Ticket.Create(request.GameId, ticket.Name, ticket.Description));
            ticketRepository.AddRange(tickets);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var mappedTickets = mapper.Map<ICollection<ImportTicketItemResponse>>(tickets);
            return new ApiCollection<ImportTicketItemResponse>(mappedTickets, mappedTickets.Count);
        }
    }
}