namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record GetTicketResponse(Guid Id, Guid GameId, string Name, string Description);
public sealed record GetTicketQuery(Guid TicketId) : IRequest<Result<GetTicketResponse, ApplicationError>>;

public static class GetTicket
{
    public class Validator : AbstractValidator<GetTicketQuery>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
        }
    }

    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Ticket, GetTicketResponse>();
        }
    }

    public sealed class RequestHandler(ITicketRepository ticketRepository, IMapper mapper)
        : IRequestHandler<GetTicketQuery, Result<GetTicketResponse, ApplicationError>>
    {
        public async Task<Result<GetTicketResponse, ApplicationError>> Handle(GetTicketQuery request, CancellationToken cancellationToken = default)
        {
            var data = await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            return mapper.Map<GetTicketResponse>(data);
        }
    }
}