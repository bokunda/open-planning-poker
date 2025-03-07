namespace OpenPlanningPoker.UserManagement.GraphQL.Features.Users;

[MutationType]
public class ChangeUsernameMutation
{
    public sealed record ChangeUsernameCommand(string Username);

    public class Validator : AbstractValidator<ChangeUsernameCommand>
    {
        public Validator()
        {
            RuleFor(c => c.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(26);
        }
    }

    public async Task<FieldResult<bool, ApplicationError>> ChangeUsernameAsync(
        [Service] ICurrentUserProvider currentUserProvider,
        [Service] IConfiguration configuration,
        [Service] HybridCache cache,
        [Required] string username,
        CancellationToken cancellationToken = default)
    {
        var validationResult = new Validator().Validate(new ChangeUsernameCommand(username));
        if (validationResult.IsValid == false)
        {
            var errors = validationResult.Errors.Select(e => new ApplicationError(e.ErrorCode, e.ErrorMessage)).ToList();
            return errors.First();
        }
        
        var currentUser = await currentUserProvider.GetAsync(cancellationToken);
        var user = new User(currentUser.Id, username);
        await cache.SetAsync(user.GetCacheKey(), user, cancellationToken: cancellationToken);

        return true;
    }
}
