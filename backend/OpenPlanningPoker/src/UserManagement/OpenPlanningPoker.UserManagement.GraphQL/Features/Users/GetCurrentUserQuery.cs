namespace OpenPlanningPoker.UserManagement.GraphQL.Features.Users;

[QueryType]
public sealed class GetCurrentUserQuery
{

    [Authorize]
    public async Task<User> GetCurrentUser(
        [Service] ICurrentUserProvider currentUserProvider, 
        CancellationToken cancellationToken)
    {
        var user = await currentUserProvider.GetAsync(cancellationToken);
        return new User(user.Id, user.UserName);
    }
}
