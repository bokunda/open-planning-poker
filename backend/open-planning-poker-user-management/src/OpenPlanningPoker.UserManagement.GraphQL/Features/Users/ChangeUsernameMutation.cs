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
        [Service] IUserService userService,
        [Service] ILogger<ChangeUsernameMutation> logger,
        [Required] string username,
        CancellationToken cancellationToken = default)
    {
        var validationResult = new Validator().Validate(new ChangeUsernameCommand(username));
        if (validationResult.IsValid == false)
        {
            var errors = validationResult.Errors.Select(e => new ApplicationError(e.ErrorCode, e.ErrorMessage)).ToList();
            return errors.First();
        }

        var currentUserId = $"{currentUserProvider.Id}";
        await userService.UpdateAsync(currentUserId, username, cancellationToken);

        logger.LogInformation("Username changed for user {userId} to {newUsername}", currentUserId, username);

        return true;
    }
}
