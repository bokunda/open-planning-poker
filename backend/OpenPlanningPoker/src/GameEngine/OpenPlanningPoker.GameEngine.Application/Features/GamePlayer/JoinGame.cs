namespace OpenPlanningPoker.GameEngine.Application.Features.GamePlayer;

public sealed record JoinGameResponse;

public sealed record JoinGameCommand(Guid GameId, Guid UserId) : IRequest<JoinGameResponse>;

public static class JoinGame
{
    public class Validator : AbstractValidator<JoinGameCommand>
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
            CreateMap<JoinGameCommand, Domain.GamePlayer.GamePlayer>();
        }
    }

    public sealed class RequestHandler(IGamePlayerRepository gamePlayerRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<JoinGameCommand, JoinGameResponse>
    {
        public async Task<JoinGameResponse> Handle(JoinGameCommand request, CancellationToken cancellationToken = default)
        {
            var gamePlayer = Domain.GamePlayer.GamePlayer.Create(request.GameId, request.UserId);
            gamePlayerRepository.Add(gamePlayer);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new JoinGameResponse();
        }
    }
}