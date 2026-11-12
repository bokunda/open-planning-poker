namespace OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

/// <summary>
/// Room options next to the deck: auto-reveal and the async voting window. Kept apart from
/// <see cref="UpdateGameSettingsCommand"/> so toggling an option never touches the deck.
/// </summary>
public sealed record UpdateGameOptionsCommand(Guid GameId, bool AutoReveal, DateTimeOffset? AsyncVotingEndsAt)
    : IRequest<Result<GetGameSettingsResponse, ApplicationError>>;

public static class UpdateGameOptions
{
    /// <summary>Longest async voting window (two weeks, one sprint).</summary>
    public static readonly TimeSpan MaxAsyncWindow = TimeSpan.FromDays(14);

    public sealed class Validator : AbstractValidator<UpdateGameOptionsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.GameId).NotEmpty();
            RuleFor(x => x.AsyncVotingEndsAt)
                .Must(endsAt => endsAt is null || endsAt <= DateTimeOffset.UtcNow.Add(MaxAsyncWindow))
                .WithMessage("The async voting window can be at most 14 days.");
        }
    }

    public sealed class RequestHandler(IGameSettingsRepository settingsRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateGameOptionsCommand, Result<GetGameSettingsResponse, ApplicationError>>
    {
        public async Task<Result<GetGameSettingsResponse, ApplicationError>> Handle(UpdateGameOptionsCommand request, CancellationToken cancellationToken)
        {
            var settings = await settingsRepository.GetByGame(request.GameId, cancellationToken);
            if (settings is null)
            {
                return new ApplicationError("GameSettingsNotFound", "Game settings not found.");
            }

            // A window that already ended means "meeting mode": store it as no window.
            var endsAt = request.AsyncVotingEndsAt > DateTimeOffset.UtcNow ? request.AsyncVotingEndsAt : null;
            settings.UpdateOptions(request.AutoReveal, endsAt);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return settings.ToGetGameSettingsResponse();
        }
    }
}
