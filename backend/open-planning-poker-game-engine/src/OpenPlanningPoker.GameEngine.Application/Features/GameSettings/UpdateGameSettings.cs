namespace OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

public sealed record UpdateGameSettingsResponse(Guid Id, Guid GameId, string DeckSetup);

public sealed record UpdateGameSettingsCommand(Guid Id, Guid GameId, string DeckSetup) : IRequest<Result<UpdateGameSettingsResponse, ApplicationError>>;

public static class UpdateGameSettings
{
    public class Validator : AbstractValidator<UpdateGameSettingsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

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
        : IRequestHandler<UpdateGameSettingsCommand, Result<UpdateGameSettingsResponse, ApplicationError>>
    {
        public async Task<Result<UpdateGameSettingsResponse, ApplicationError>> Handle(UpdateGameSettingsCommand request, CancellationToken cancellationToken = default)
        {
            // Create a game
            var gameSettings = await gameSettingsRepository.GetByIdAsync(request.Id, cancellationToken);
            if (gameSettings is null || gameSettings.GameId != request.GameId)
            {
                return new ApplicationError("GameSettingsNotFound", "Game settings not found for this game.");
            }

            gameSettings.Update(request.GameId, request.DeckSetup);
            gameSettingsRepository.Update(gameSettings);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return gameSettings.ToUpdateGameSettingsResponse();
        }
    }
}