namespace OpenPlanningPoker.Shared.Models;

public class BaseUserProfile(Guid id, string userName)
{
    public Guid Id { get; set; } = id;
    public string UserName { get; set; } = userName;

    public string GetCacheKey() => $"User:{Id}";
    public static string GetCacheKey(Guid id) => $"User:{id}";
}
