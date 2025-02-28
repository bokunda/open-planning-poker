namespace OpenPlanningPoker.UserManagement.GraphQL.Tests.Users;

public class RegisterUserTests
{
    private const string Mutation = """
            mutation RegisterUser($input: RegisterUserInput!) {
                registerUser(input: $input) {
                  user {
                    username
                    __typename
                  }
                  errors {
                    ... on OpenPlanningPokerError {
                      code
                      message
                      __typename
                    }
                  }
                }
            }
        """;

    private Dictionary<string, object?> GetVariables(string username) => new()
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
        const string Username = "Username123";


        var usernameGeneratorService = Substitute.For<IUsernameGeneratorService>();
        usernameGeneratorService.GenerateUsername(Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .Returns(Username);

        var executor = await new ServiceCollection()
            .AddSingleton(usernameGeneratorService)
            .AddGraphQL()
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
        const string Username = "";

        var usernameGeneratorService = Substitute.For<IUsernameGeneratorService>();
        usernameGeneratorService.GenerateUsername(Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .Returns("Username123");

        var executor = await new ServiceCollection()
            .AddSingleton(usernameGeneratorService)
            .AddGraphQL()
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

        var usernameGeneratorService = Substitute.For<IUsernameGeneratorService>();
        var executor = await new ServiceCollection()
            .AddSingleton(usernameGeneratorService)
            .AddGraphQL()
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
