namespace OpenPlanningPoker.UserManagement.GraphQL.Features.Users;

public class User(Guid id, string username)
{
    public Guid Id { get; private set; } = id;
    public string UserName { get; private set; } = username;

    internal string GetCacheKey() => $"User:{Id}";
}
