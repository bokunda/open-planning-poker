namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record UpdateTicketResponse(Guid Id, Guid GameId, string Name, string Description);

public sealed record UpdateTicketCommand(Guid TicketId, string Name, string Description) : IRequest<Result<UpdateTicketResponse, ApplicationError>>;

public static class UpdateTicket
{
    public class Validator : AbstractValidator<UpdateTicketCommand>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId)
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
            CreateMap<UpdateTicketCommand, Ticket>();
            CreateMap<Ticket, UpdateTicketResponse>();
        }
    }

    public sealed class RequestHandler(ITicketRepository ticketRepository, IMapper mapper, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateTicketCommand, Result<UpdateTicketResponse, ApplicationError>>
    {
        public async Task<Result<UpdateTicketResponse, ApplicationError>> Handle(UpdateTicketCommand request, CancellationToken cancellationToken = default)
        {
            var ticket = await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            ticket!.Update(request.Name, request.Description);

            ticketRepository.Update(ticket);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<UpdateTicketResponse>(ticket);
        }
    }
}