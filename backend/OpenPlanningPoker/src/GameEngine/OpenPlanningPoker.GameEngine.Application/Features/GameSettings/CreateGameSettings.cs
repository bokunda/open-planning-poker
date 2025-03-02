namespace OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

public sealed record CreateGameSettingsResponse(Guid Id, Guid GameId, int VotingTime, bool IsBreakAllowed);

public sealed record CreateGameSettingsCommand(Guid GameId, int VotingTime, bool IsBreakAllowed) : IRequest<Result<CreateGameSettingsResponse, ApplicationError>>;

public static class CreateGameSettings
{
    public class Validator : AbstractValidator<CreateGameSettingsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.GameId)
                .NotEmpty();

            RuleFor(x => x.VotingTime)
                .GreaterThan(0);

            RuleFor(x => x.IsBreakAllowed)
                .NotEmpty();
        }
    }

    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateGameSettingsCommand, Domain.GameSettings.GameSettings>();
            CreateMap<Domain.GameSettings.GameSettings, CreateGameSettingsResponse>();
        }
    }

    public sealed class RequestHandler(
        IGameSettingsRepository gameSettingsRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
        : IRequestHandler<CreateGameSettingsCommand, Result<CreateGameSettingsResponse, ApplicationError>>
    {
        public async Task<Result<CreateGameSettingsResponse, ApplicationError>> Handle(CreateGameSettingsCommand request, CancellationToken cancellationToken = default)
        {
            var game = Domain.GameSettings.GameSettings.Create(request.GameId, request.VotingTime, request.IsBreakAllowed);
            gameSettingsRepository.Add(game);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<CreateGameSettingsResponse>(game);
        }
    }
}