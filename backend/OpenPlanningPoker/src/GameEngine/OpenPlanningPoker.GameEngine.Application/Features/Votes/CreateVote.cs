namespace OpenPlanningPoker.GameEngine.Application.Features.Votes;

public sealed record CreateVoteResponse(Guid Id, Guid PlayerId, Guid TicketId, string Value);

public sealed record CreateVoteCommand(Guid TicketId, string Value) : IRequest<Result<CreateVoteResponse, ApplicationError>>;

public static class CreateVote
{
    public class Validator : AbstractValidator<CreateVoteCommand>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty();

            RuleFor(x => x.Value)
                .NotEmpty();
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
        : IRequestHandler<CreateVoteCommand, Result<CreateVoteResponse, ApplicationError>>
    {
        public async Task<Result<CreateVoteResponse, ApplicationError>> Handle(CreateVoteCommand request, CancellationToken cancellationToken = default)
        {
            var vote = Vote.Create(currentUserProvider.Id, request.TicketId, request.Value);
            voteRepository.Add(vote);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<CreateVoteResponse>(vote);
        }
    }
}