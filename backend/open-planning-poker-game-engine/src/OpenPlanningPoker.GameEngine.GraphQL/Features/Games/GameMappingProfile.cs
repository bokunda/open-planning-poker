namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

public class GameMappingProfile : Profile
{
    public GameMappingProfile()
    {
        CreateMap<GetGameResponse, Game>()
            .ForMember(game => game.HostId, options => options.MapFrom(source => source.CreatedBy));
        CreateMap<CreateGameResponse, Game>()
            .ForMember(game => game.HostId, options => options.MapFrom(source => source.CreatedBy));
    }
}
