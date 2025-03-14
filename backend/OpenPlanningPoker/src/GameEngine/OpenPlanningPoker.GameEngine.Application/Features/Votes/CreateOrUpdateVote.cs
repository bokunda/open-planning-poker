namespace OpenPlanningPoker.GameEngine.Application.Features.Votes;

public sealed record CreateOrUpdateVoteResponse(Guid Id, Guid PlayerId, Guid TicketId, string Value);

public sealed record CreateOrUpdateVoteCommand(Guid TicketId, string Value) : IRequest<Result<CreateOrUpdateVoteResponse, ApplicationError>>;

public static class CreateOrUpdateVote
{
    public class Validator : AbstractValidator<CreateOrUpdateVoteCommand>
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
            CreateMap<CreateOrUpdateVoteCommand, Vote>();
            CreateMap<Vote, CreateOrUpdateVoteResponse>();
        }
    }

    public sealed class RequestHandler(
        IVoteRepository voteRepository,
        ICurrentUserProvider currentUserProvider,
        IMapper mapper,
        IUnitOfWork unitOfWork)
        : IRequestHandler<CreateOrUpdateVoteCommand, Result<CreateOrUpdateVoteResponse, ApplicationError>>
    {
        public async Task<Result<CreateOrUpdateVoteResponse, ApplicationError>> Handle(CreateOrUpdateVoteCommand request, CancellationToken cancellationToken = default)
        {
            var vote = (await voteRepository.GetByTicket(request.TicketId, currentUserProvider.Id, cancellationToken)).FirstOrDefault();

            if (vote is not null)
            {
                vote!.Update(request.Value);
                voteRepository.Update(vote);
            }
            else
            {
                vote = Vote.Create(currentUserProvider.Id, request.TicketId, request.Value);
                voteRepository.Add(vote);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return mapper.Map<CreateOrUpdateVoteResponse>(vote);
        }
    }
}