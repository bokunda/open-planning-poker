namespace OpenPlanningPoker.Shared.Models;

public class BaseUserProfile(Guid id, string userName)
{
    public Guid Id { get; set; } = id;
    public string UserName { get; set; } = userName;

    [GraphQLIgnore]
    public string GetCacheKey() => $"User:{Id}";
    [GraphQLIgnore]
    public static string GetCacheKey(Guid id) => $"User:{id}";
}
