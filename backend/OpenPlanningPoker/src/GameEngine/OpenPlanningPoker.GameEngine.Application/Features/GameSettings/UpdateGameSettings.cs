namespace OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

public sealed record UpdateGameSettingsResponse(Guid Id, Guid GameId, int VotingTime, bool IsBreakAllowed);

public sealed record UpdateGameSettingsCommand(Guid Id, Guid GameId, int VotingTime, bool IsBreakAllowed) : IRequest<UpdateGameSettingsResponse>;

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
            CreateMap<Domain.GameSettings.GameSettings, UpdateGameSettingsResponse>();
        }
    }

    public sealed class RequestHandler(
        IGameSettingsRepository gameSettingsRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateGameSettingsCommand, UpdateGameSettingsResponse>
    {
        public async Task<UpdateGameSettingsResponse> Handle(UpdateGameSettingsCommand request, CancellationToken cancellationToken = default)
        {
            // Create a game
            var gameSettings = await gameSettingsRepository.GetByIdAsync(request.Id, cancellationToken);
            gameSettings!.Update(request.GameId, request.VotingTime, request.IsBreakAllowed);
            gameSettingsRepository.Update(gameSettings);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<UpdateGameSettingsResponse>(gameSettings);
        }
    }
}