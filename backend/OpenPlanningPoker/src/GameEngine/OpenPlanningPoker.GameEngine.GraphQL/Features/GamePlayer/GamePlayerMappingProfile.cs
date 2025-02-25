namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer
{
    public class GamePlayerMappingProfile : Profile
    {
        public GamePlayerMappingProfile()
        {
            CreateMap<ListPlayersItem, GamePlayer>();
            CreateMap<ListPlayersItem, GamePlayer>();
        }
    }
}
