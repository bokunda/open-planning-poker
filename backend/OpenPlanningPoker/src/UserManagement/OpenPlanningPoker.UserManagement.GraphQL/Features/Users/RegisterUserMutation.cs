

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

    public async Task<FieldResult<RegisterUserResponse, ApplicationError>> RegisterUserAsync(
        [Service] IUsernameGeneratorService usernameGeneratorService,
        [Service] IUserService userService,
        [Service] IConfiguration configuration,
        [Service] ILogger<RegisterUserMutation> logger,
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

        var user = await userService.AddAsync(username, cancellationToken);

        var registeredUser = new RegisterUserResponse(user.Id, user.UserName);

        var token = GenerateToken(registeredUser.Id.ToString(), configuration["Authentication:Secret"]!);
        registeredUser.SetToken(token);

        logger.LogInformation("User registered with id: {userId} and username: {newUsername}", user.Id, username);

        return registeredUser;
    }

    private static string GenerateToken(string id, string secretKey)
    {
        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(nameof(id), id)]),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token) ?? throw new Exception("Cannot create a token!");
    }
}
