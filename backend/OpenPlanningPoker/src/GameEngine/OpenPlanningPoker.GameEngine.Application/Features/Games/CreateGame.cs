namespace OpenPlanningPoker.GameEngine.Application.Features.Games;

public sealed record CreateGameResponse(Guid Id, string Name, string Description);

public sealed record CreateGameCommand(string Name, string Description) : IRequest<CreateGameResponse>;

public static class CreateGame
{
    public class Validator : AbstractValidator<CreateGameCommand>
    {
        public Validator()
        {
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
            CreateMap<CreateGameCommand, Game>();
            CreateMap<Game, CreateGameResponse>();
        }
    }

    public sealed class RequestHandler(IGameRepository gameRepository, IMapper mapper, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateGameCommand, CreateGameResponse>
    {
        public async Task<CreateGameResponse> Handle(CreateGameCommand request, CancellationToken cancellationToken = default)
        {
            // Create a game
            var game = Game.Create(request.Name, request.Description);
            gameRepository.Add(game);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<CreateGameResponse>(game);
        }
    }
}