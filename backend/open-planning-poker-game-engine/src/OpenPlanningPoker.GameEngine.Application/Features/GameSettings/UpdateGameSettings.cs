namespace OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

public sealed record UpdateGameSettingsResponse(Guid Id, Guid GameId, string DeckSetup, bool AutoReveal = false, DateTimeOffset? AsyncVotingEndsAt = null);

public sealed record UpdateGameSettingsCommand(Guid Id, Guid GameId, string DeckSetup) : IRequest<Result<UpdateGameSettingsResponse, ApplicationError>>, IAuditedRequest
{
    public AuditEntry? ToAuditEntry() => AuditEntry.Game(AuditActions.SettingsDeck, AuditType.Update, GameId, value: DeckSetup);
}

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
        IUnitOfWork unitOfWork,
        AuditContext audit)
        : IRequestHandler<UpdateGameSettingsCommand, Result<UpdateGameSettingsResponse, ApplicationError>>
    {
        public async Task<Result<UpdateGameSettingsResponse, ApplicationError>> Handle(UpdateGameSettingsCommand request, CancellationToken cancellationToken = default)
        {
            var gameSettings = await gameSettingsRepository.GetByIdAsync(request.Id, cancellationToken);
            if (gameSettings is null || gameSettings.GameId != request.GameId)
            {
                return new ApplicationError("GameSettingsNotFound", "Game settings not found for this game.");
            }

            // The room saves the chosen deck right after creating a game; with the default deck that is no change.
            if (gameSettings.DeckSetup == request.DeckSetup)
            {
                audit.MarkUnchanged();
                return gameSettings.ToUpdateGameSettingsResponse();
            }

            gameSettings.Update(request.GameId, request.DeckSetup);
            gameSettingsRepository.Update(gameSettings);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return gameSettings.ToUpdateGameSettingsResponse();
        }
    }
}