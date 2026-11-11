namespace OpenPlanningPoker.GameEngine.Application.Features.Votes;

public sealed record GetVotesItem(Guid Id, Guid PlayerId, string Value);
public sealed record GetVotesQuery(Guid TicketId) : IRequest<Result<ApiCollection<GetVotesItem>, ApplicationError>>;

public static class GetVotes
{
    /// <summary>Value returned in place of a vote that has not been revealed yet.</summary>
    public const string HiddenVoteValue = "";

    public class Validator : AbstractValidator<GetVotesQuery>
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
            CreateMap<Vote, GetVotesItem>();
        }
    }

    public sealed class RequestHandler(
        IVoteRepository voteRepository,
        ITicketRepository ticketRepository,
        ICurrentUserProvider currentUserProvider,
        IMapper mapper)
        : IRequestHandler<GetVotesQuery, Result<ApiCollection<GetVotesItem>, ApplicationError>>
    {
        public async Task<Result<ApiCollection<GetVotesItem>, ApplicationError>> Handle(GetVotesQuery request, CancellationToken cancellationToken = default)
        {
            var ticket = await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return new ApiCollection<GetVotesItem>([], 0);
            }

            var result = await voteRepository.GetByTicket(request.TicketId, null, cancellationToken);
            var mappedResult = mapper.Map<ICollection<GetVotesItem>>(result);

            if (!ticket.VotesRevealed)
            {
                // Until the host reveals, players only learn who voted, not what they voted.
                var currentUserId = currentUserProvider.Id;
                mappedResult = mappedResult
                    .Select(vote => vote.PlayerId == currentUserId && currentUserId != Guid.Empty
                        ? vote
                        : vote with { Value = HiddenVoteValue })
                    .ToList();
            }

            return new ApiCollection<GetVotesItem>(mappedResult, mappedResult.Count);
        }
    }
}
