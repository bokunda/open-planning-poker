namespace OpenPlanningPoker.GameEngine.Application.Features.Votes;

public sealed record CreateVoteResponse(Guid Id, Guid PlayerId, Guid TicketId, int Value);

public sealed record CreateVoteCommand(Guid TicketId, int Value) : IRequest<CreateVoteResponse>;

public static class CreateVote
{
    public class Validator : AbstractValidator<CreateVoteCommand>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty();

            RuleFor(x => x.Value)
                .GreaterThan(0)
                .LessThan(100);
        }
    }

    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateVoteCommand, Vote>();
            CreateMap<Vote, CreateVoteResponse>();
        }
    }

    public sealed class RequestHandler(
        IVoteRepository voteRepository,
        ICurrentUserProvider currentUserProvider,
        IMapper mapper,
        IUnitOfWork unitOfWork)
        : IRequestHandler<CreateVoteCommand, CreateVoteResponse>
    {
        public async Task<CreateVoteResponse> Handle(CreateVoteCommand request, CancellationToken cancellationToken = default)
        {
            var vote = Vote.Create(currentUserProvider.CustomerId, request.TicketId, request.Value);
            voteRepository.Add(vote);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<CreateVoteResponse>(vote);
        }
    }
}