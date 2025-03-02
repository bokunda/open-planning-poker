namespace OpenPlanningPoker.GameEngine.Application.Features.Votes;

public sealed record UpdateVoteResponse(Guid Id, Guid PlayerId, Guid TicketId, int Value);

public sealed record UpdateVoteCommand(Guid Id, int Value) : IRequest<Result<UpdateVoteResponse, ApplicationError>>;

public static class UpdateVote
{
    public class Validator : AbstractValidator<UpdateVoteCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
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
            CreateMap<UpdateVoteCommand, Vote>();
            CreateMap<Vote, UpdateVoteResponse>();
        }
    }

    public sealed class RequestHandler(
        IVoteRepository voteRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateVoteCommand, Result<UpdateVoteResponse, ApplicationError>>
    {
        public async Task<Result<UpdateVoteResponse, ApplicationError>> Handle(UpdateVoteCommand request, CancellationToken cancellationToken = default)
        {
            var vote = await voteRepository.GetByIdAsync(request.Id, cancellationToken);
            vote!.Update(request.Value);
            voteRepository.Update(vote);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<UpdateVoteResponse>(vote);
        }
    }
}