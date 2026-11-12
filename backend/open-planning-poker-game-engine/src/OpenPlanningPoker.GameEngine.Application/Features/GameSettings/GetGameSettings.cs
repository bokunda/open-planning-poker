namespace OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

public sealed record GetGameSettingsResponse(Guid Id, Guid GameId, string DeckSetup, bool AutoReveal = false, DateTimeOffset? AsyncVotingEndsAt = null);
public sealed record GetGameSettingsQuery(Guid GameId) : IRequest<Result<GetGameSettingsResponse, ApplicationError>>;

public static class GetGameSettings
{
    public class Validator : AbstractValidator<GetGameSettingsQuery>
    {
        public Validator()
        {
            RuleFor(x => x.GameId).NotEmpty();
        }
    }


    public sealed class RequestHandler(IGameSettingsRepository gameSettingsRepository)
        : IRequestHandler<GetGameSettingsQuery, Result<GetGameSettingsResponse, ApplicationError>>
    {
        public async Task<Result<GetGameSettingsResponse, ApplicationError>> Handle(GetGameSettingsQuery request, CancellationToken cancellationToken = default)
        {
            var data = await gameSettingsRepository.GetByGame(request.GameId, cancellationToken);
            if (data is null)
            {
                return new ApplicationError("GameSettingsNotFound", "Game settings not found.");
            }

            return data.ToGetGameSettingsResponse();
        }
    }
}