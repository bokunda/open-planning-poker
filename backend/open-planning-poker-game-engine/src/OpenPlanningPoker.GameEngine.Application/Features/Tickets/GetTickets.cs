namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

public sealed record GetTicketsItem(Guid Id, Guid GameId, string Name, string Description, bool VotesRevealed);
public sealed record GetTicketsQuery(Guid GameId) : IRequest<Result<ApiCollection<GetTicketsItem>, ApplicationError>>;

public static class GetTickets
{
    public class Validator : AbstractValidator<GetTicketsQuery>
    {
        public Validator()
        {
            RuleFor(x => x.GameId).NotEmpty();
        }
    }


    public sealed class RequestHandler(ITicketRepository ticketRepository)
        : IRequestHandler<GetTicketsQuery, Result<ApiCollection<GetTicketsItem>, ApplicationError>>
    {
        public async Task<Result<ApiCollection<GetTicketsItem>, ApplicationError>> Handle(GetTicketsQuery request, CancellationToken cancellationToken = default)
        {
            var data = await ticketRepository.GetByGame(request.GameId, cancellationToken);
            var mappedData = data.Select(item => item.ToGetTicketsItem()).ToList();
            return new ApiCollection<GetTicketsItem>(mappedData, mappedData.Count);
        }
    }
}