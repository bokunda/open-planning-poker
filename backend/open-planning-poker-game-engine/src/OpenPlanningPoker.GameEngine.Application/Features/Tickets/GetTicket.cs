namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record GetTicketResponse(Guid Id, Guid GameId, string Name, string Description, bool VotesRevealed);
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


    public sealed class RequestHandler(ITicketRepository ticketRepository)
        : IRequestHandler<GetTicketQuery, Result<GetTicketResponse, ApplicationError>>
    {
        public async Task<Result<GetTicketResponse, ApplicationError>> Handle(GetTicketQuery request, CancellationToken cancellationToken = default)
        {
            var data = await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (data is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            return data.ToGetTicketResponse();
        }
    }
}