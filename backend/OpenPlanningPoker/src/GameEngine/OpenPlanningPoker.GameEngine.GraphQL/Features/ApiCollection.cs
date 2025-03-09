namespace OpenPlanningPoker.GameEngine.GraphQL.Features;

public sealed record ApiCollection<T>(ICollection<T> Items, int TotalCount);

public class ApiCollectionMappingProfile : Profile
{
    public ApiCollectionMappingProfile()
    {
        CreateMap(typeof(Application.ApiCollection<>), typeof(ApiCollection<>));
        CreateMap(typeof(ApiCollection<>), typeof(Application.ApiCollection<>));
    }
}
