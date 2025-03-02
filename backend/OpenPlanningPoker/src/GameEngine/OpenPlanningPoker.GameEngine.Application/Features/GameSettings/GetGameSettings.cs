namespace OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

public sealed record GetGameSettingsResponse(Guid Id, Guid GameId, int VotingTime, bool IsBreakAllowed);
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

    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Domain.GameSettings.GameSettings, GetGameSettingsResponse>();
        }
    }

    public sealed class RequestHandler(IGameSettingsRepository gameSettingsRepository, IMapper mapper)
        : IRequestHandler<GetGameSettingsQuery, Result<GetGameSettingsResponse, ApplicationError>>
    {
        public async Task<Result<GetGameSettingsResponse, ApplicationError>> Handle(GetGameSettingsQuery request, CancellationToken cancellationToken = default)
        {
            var data = await gameSettingsRepository.GetByGame(request.GameId, cancellationToken);
            return mapper.Map<GetGameSettingsResponse>(data);
        }
    }
}