namespace OpenPlanningPoker.UserManagement.GraphQL.Features.Users;

public sealed class RegisterUserResponse(Guid id, string username) : User(id, username)
{
    public string Token { get; private set; } = string.Empty;
    public void SetToken(string token) => Token = token;
}
