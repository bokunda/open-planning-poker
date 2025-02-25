namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record GetTicketsItem(Guid Id, Guid GameId, string Name, string Description);
public sealed record GetTicketsQuery(Guid GameId) : IRequest<ApiCollection<GetTicketsItem>>;

public static class GetTickets
{
    public class Validator : AbstractValidator<GetTicketsQuery>
    {
        public Validator()
        {
            RuleFor(x => x.GameId).NotEmpty();
        }
    }

    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Ticket, GetTicketsItem>();
        }
    }

    public sealed class RequestHandler(ITicketRepository ticketRepository, IMapper mapper)
        : IRequestHandler<GetTicketsQuery, ApiCollection<GetTicketsItem>>
    {
        public async Task<ApiCollection<GetTicketsItem>> Handle(GetTicketsQuery request, CancellationToken cancellationToken = default)
        {
            var data = await ticketRepository.GetByGame(request.GameId, cancellationToken);
            var mappedData = mapper.Map<ICollection<GetTicketsItem>>(data);
            return new ApiCollection<GetTicketsItem>(mappedData, mappedData.Count);
        }
    }
}