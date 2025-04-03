namespace OpenPlanningPoker.GameEngine.Application.Features.Games;

public sealed record DeleteGameCommand(Guid GameId) : IRequest<Result<bool, ApplicationError>>;

/// <summary>
/// Delete Game will be used by the service worker for DB cleanup purposes so no validation is needed.
/// </summary>
public static class DeleteGame
{
    public class Validator : AbstractValidator<DeleteGameCommand>
    {
        public Validator()
        {
            RuleFor(x => x.GameId)
                .NotEmpty();
        }
    }

    public sealed class RequestHandler(IGameRepository gameRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteGameCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(DeleteGameCommand request, CancellationToken cancellationToken = default)
        {
            var game = (await gameRepository.GetByIdAsync(request.GameId, cancellationToken))!;
            gameRepository.Delete(game);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}