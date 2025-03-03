using Microsoft.Extensions.Caching.Hybrid;

namespace OpenPlanningPoker.UserManagement.GraphQL.Features.Users;

[MutationType]
public class RegisterUserMutation
{
    public sealed record RegisterUserCommand(string Username);

    public class Validator : AbstractValidator<RegisterUserCommand>
    {
        public Validator()
        {
            RuleFor(c => c.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(26);
        }
    }

    public async Task<FieldResult<User, ApplicationError>> RegisterUserAsync(
        [Service] IUsernameGeneratorService usernameGeneratorService,
        [Service] HybridCache cache,
        string? username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(username))
        {
            username = await usernameGeneratorService.GenerateUsername(Language.English, cancellationToken);
        }

        var validationResult = new Validator().Validate(new RegisterUserCommand(username));
        if (validationResult.IsValid == false)
        {
            var errors = validationResult.Errors.Select(e => new ApplicationError(e.ErrorCode, e.ErrorMessage)).ToList();
            return errors.First();
        }
        
        var user = new User(Guid.NewGuid(), username);

        await cache.SetAsync(user.GetCacheKey(), user, cancellationToken: cancellationToken);

        return user;
    }
}
