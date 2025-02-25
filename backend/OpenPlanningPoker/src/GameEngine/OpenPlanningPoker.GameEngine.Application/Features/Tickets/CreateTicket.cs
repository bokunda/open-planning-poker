namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record CreateTicketResponse(Guid Id, Guid GameId, string Name, string Description);

public sealed record CreateTicketCommand(Guid GameId, string Name, string Description) : IRequest<CreateTicketResponse>;

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

    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateTicketCommand, Ticket>();
            CreateMap<Ticket, CreateTicketResponse>();
        }
    }

    public sealed class RequestHandler(ITicketRepository ticketRepository, IMapper mapper, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateTicketCommand, CreateTicketResponse>
    {
        public async Task<CreateTicketResponse> Handle(CreateTicketCommand request, CancellationToken cancellationToken = default)
        {
            var ticket = Ticket.Create(request.GameId, request.Name, request.Description);
            ticketRepository.Add(ticket);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<CreateTicketResponse>(ticket);
        }
    }
}