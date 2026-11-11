namespace OpenPlanningPoker.GameEngine.Application.Features.Games;

public sealed record CreateGameResponse(Guid Id, string Name, string Description, Guid CreatedBy);

public sealed record CreateGameCommand(string Name, string Description) : IRequest<Result<CreateGameResponse, ApplicationError>>;

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


    public sealed class RequestHandler(IGameRepository gameRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateGameCommand, Result<CreateGameResponse, ApplicationError>>
    {
        public async Task<Result<CreateGameResponse, ApplicationError>> Handle(CreateGameCommand request, CancellationToken cancellationToken = default)
        {
            // Create a game
            var game = Game.Create(request.Name, request.Description);
            gameRepository.Add(game);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return game.ToCreateGameResponse();
        }
    }
}