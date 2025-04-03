namespace OpenPlanningPoker.UserManagement.GraphQL.Features.Users;

[QueryType]
public sealed class GetCurrentUserQuery
{

    [Authorize]
    public async Task<FieldResult<User, ApplicationError>> GetCurrentUser(
        [Service] ICurrentUserProvider currentUserProvider, 
        CancellationToken cancellationToken)
    {
        var user = await currentUserProvider.GetAsync(cancellationToken);

        return user.IsSuccess
            ? new User(user.Value!.Id, user.Value.UserName)
            : user.Error!;
    }
}
