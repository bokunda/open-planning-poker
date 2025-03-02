namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Settings;

public class SettingsMappingProfile : Profile
{
    public SettingsMappingProfile()
    {
        CreateMap<GetGameSettingsResponse, Settings>();
        CreateMap<CreateGameSettingsResponse, Settings>();
        CreateMap<UpdateGameSettingsResponse, Settings>();
    }
}
