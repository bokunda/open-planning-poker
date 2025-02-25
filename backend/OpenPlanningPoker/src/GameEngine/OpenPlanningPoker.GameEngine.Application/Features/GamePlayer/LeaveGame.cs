namespace OpenPlanningPoker.GameEngine.Application.Features.GamePlayer;

public sealed record LeaveGameResponse;

public sealed record LeaveGameCommand(Guid GameId, Guid UserId) : IRequest<LeaveGameResponse>;

public static class LeaveGame
{
    public class Validator : AbstractValidator<LeaveGameCommand>
    {
        public Validator()
        {
            RuleFor(x => x.GameId)
                .NotEmpty();

            RuleFor(x => x.UserId)
                .NotEmpty();
        }
    }

    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<LeaveGameCommand, Domain.GamePlayer.GamePlayer>();
        }
    }

    public sealed class RequestHandler(IGamePlayerRepository gamePlayerRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<LeaveGameCommand, LeaveGameResponse>
    {
        public async Task<LeaveGameResponse> Handle(LeaveGameCommand request, CancellationToken cancellationToken = default)
        {
            var result = await gamePlayerRepository.GetByGameAndPlayer(request.GameId, request.UserId, cancellationToken);
            gamePlayerRepository.Delete(result);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new LeaveGameResponse();
        }
    }
}