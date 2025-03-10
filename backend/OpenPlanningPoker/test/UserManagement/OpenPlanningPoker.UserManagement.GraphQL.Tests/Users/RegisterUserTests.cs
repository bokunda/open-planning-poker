using Microsoft.Extensions.Configuration;

namespace OpenPlanningPoker.UserManagement.GraphQL.Tests.Users;

public class RegisterUserTests
{
    private const string AuthenticationSecret = "e65fb783-3952-411c-b41c-1d5dc1d9ef6c--e65fb783-3952-411c-b41c-1d5dc1d9ef6c";
    private const string Mutation = """
        mutation RegisterUser($input: RegisterUserInput!) {
          registerUser(input: $input) {
            registerUserResponse {
              id
              userName
              token
              __typename
            }
            errors {
              ... on ApplicationError {
                code
                message
                __typename
              }
            }
            __typename
          }
        }
        """;

    private static Dictionary<string, object?> GetVariables(string username) => new()
    {
        ["input"] = new Dictionary<string, object?>
        {
            ["username"] = username
        }
    };

    [Fact]
    public async Task RegisterUser_Success()
    {
        // arrange
        var userId = Guid.Parse("e65fb783-3952-411c-b41c-1d5dc1d9ef6c");
        const string Username = "Username123";


        var usernameGeneratorService = Substitute.For<IUsernameGeneratorService>();
        usernameGeneratorService.GenerateUsername(Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .Returns(Username);

        var userService = Substitute.For<IUserService>();
        userService.AddAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BaseUserProfile(userId, Username));

        var configuration = Substitute.For<IConfiguration>();
        configuration["Authentication:Secret"] = AuthenticationSecret;

        var executor = await new ServiceCollection()
            .AddSingleton(usernameGeneratorService)
            .AddSingleton(userService)
            .AddSingleton(configuration)
            .AddLogging()
            .AddGraphQL()
            .AddAuthorization()
            .AddQueryConventions()
            .AddMutationConventions()
            .AddTypes()
            .BuildRequestExecutorAsync();

        var variables = GetVariables(Username);

        // act
        var result = await executor.ExecuteAsync(Mutation, variables);


        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task RegisterUser_NoUsername_Success()
    {
        // arrange
        var userId = Guid.Parse("e65fb783-3952-411c-b41c-1d5dc1d9ef6c");
        const string Username = "";
        const string GeneratedUsername = "Username123";

        var usernameGeneratorService = Substitute.For<IUsernameGeneratorService>();
        usernameGeneratorService.GenerateUsername(Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .Returns(GeneratedUsername);

        var userService = Substitute.For<IUserService>();
        userService.AddAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BaseUserProfile(userId, GeneratedUsername));

        var configuration = Substitute.For<IConfiguration>();
        configuration["Authentication:Secret"] = AuthenticationSecret;

        var executor = await new ServiceCollection()
            .AddSingleton(usernameGeneratorService)
            .AddSingleton(userService)
            .AddSingleton(configuration)
            .AddLogging()
            .AddGraphQL()
            .AddAuthorization()
            .AddQueryConventions()
            .AddMutationConventions()
            .AddTypes()
            .BuildRequestExecutorAsync();

        var variables = GetVariables(Username);

        // act
        var result = await executor.ExecuteAsync(Mutation, variables);


        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task RegisterUser_ToManyCharacters_Failed()
    {
        // arrange
        const string Username = "Username123Username123Username123Username123Username123Username123";

        var userService = Substitute.For<IUserService>();
        var usernameGeneratorService = Substitute.For<IUsernameGeneratorService>();
        var configuration = Substitute.For<IConfiguration>();
        configuration["Authentication:Secret"] = AuthenticationSecret;

        var executor = await new ServiceCollection()
            .AddSingleton(usernameGeneratorService)
            .AddSingleton(userService)
            .AddSingleton(configuration)
            .AddLogging()
            .AddGraphQL()
            .AddAuthorization()
            .AddQueryConventions()
            .AddMutationConventions()
            .AddTypes()
            .BuildRequestExecutorAsync();

        var variables = GetVariables(Username);

        // act
        var result = await executor.ExecuteAsync(Mutation, variables);


        // assert
        result.MatchSnapshot();
    }
}
