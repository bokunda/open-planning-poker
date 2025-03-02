namespace OpenPlanningPoker.GameEngine.Application.Features.Games;

public sealed record GetGameResponse(Guid Id, string Name, string Description);
public sealed record GetGameQuery(Guid GameId) : IRequest<Result<GetGameResponse, ApplicationError>>;

public static class GetGame
{
    public class Validator : AbstractValidator<GetGameQuery>
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
            CreateMap<Game, GetGameResponse>();
        }
    }

    public sealed class RequestHandler(IGameRepository gameRepository, IMapper mapper)
        : IRequestHandler<GetGameQuery, Result<GetGameResponse, ApplicationError>>
    {
        public async Task<Result<GetGameResponse, ApplicationError>> Handle(GetGameQuery request, CancellationToken cancellationToken = default)
        {
            var data = await gameRepository.GetByIdAsync(request.GameId, cancellationToken);
            return mapper.Map<GetGameResponse>(data);
        }
    }
}