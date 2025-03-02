namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

public class GameMappingProfile : Profile
{
    public GameMappingProfile()
    {
        CreateMap<GetGameResponse, Game>();
        CreateMap<CreateGameResponse, Game>();
    }
}
