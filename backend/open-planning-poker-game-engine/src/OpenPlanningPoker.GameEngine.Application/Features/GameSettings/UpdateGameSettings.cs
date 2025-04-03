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
                .Must(BeValidCsv).WithMessage("Deck must be in the CSV format.");
        }

        private bool BeValidCsv(string deckSetup) => deckSetup.Split(',').All(item => !string.IsNullOrWhiteSpace(item));
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
        : IRequestHandler<UpdateGameSettingsCommand, Result<UpdateGameSettingsResponse, ApplicationError>>
    {
        public async Task<Result<UpdateGameSettingsResponse, ApplicationError>> Handle(UpdateGameSettingsCommand request, CancellationToken cancellationToken = default)
        {
            // Create a game
            var gameSettings = await gameSettingsRepository.GetByIdAsync(request.Id, cancellationToken);
            gameSettings!.Update(request.GameId, request.DeckSetup);
            gameSettingsRepository.Update(gameSettings);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<UpdateGameSettingsResponse>(gameSettings);
        }
    }
}