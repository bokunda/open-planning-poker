namespace OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

public sealed record CreateGameSettingsResponse(Guid Id, Guid GameId, string DeckSetup);

public sealed record CreateGameSettingsCommand(Guid GameId, string DeckSetup) : IRequest<Result<CreateGameSettingsResponse, ApplicationError>>;

public static class CreateGameSettings
{
    public class Validator : AbstractValidator<CreateGameSettingsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.GameId)
                .NotEmpty();

            RuleFor(x => x.DeckSetup)
                .NotEmpty()
                .Must(GameSettingsConstants.IsValidDeck)
                .WithMessage($"Deck must have 2-{GameSettingsConstants.MaxDeckCards} unique comma-separated cards of up to {GameSettingsConstants.MaxCardLength} characters.");
        }
    }


    public sealed class RequestHandler(
        IGameSettingsRepository gameSettingsRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<CreateGameSettingsCommand, Result<CreateGameSettingsResponse, ApplicationError>>
    {
        public async Task<Result<CreateGameSettingsResponse, ApplicationError>> Handle(CreateGameSettingsCommand request, CancellationToken cancellationToken = default)
        {
            var game = Domain.GameSettings.GameSettings.Create(request.GameId, request.DeckSetup);
            gameSettingsRepository.Add(game);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return game.ToCreateGameSettingsResponse();
        }
    }
}