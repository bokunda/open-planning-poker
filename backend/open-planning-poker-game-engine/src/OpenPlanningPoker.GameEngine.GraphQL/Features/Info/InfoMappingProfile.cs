namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Info;

public class InfoMappingProfile : Profile
{
    public InfoMappingProfile()
    {
        CreateMap<GetInfoResponse, Info>();
    }
}
