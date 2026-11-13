using System.Globalization;
using Microsoft.Extensions.Configuration;
using OpenPlanningPoker.UserManagement.GraphQL.Localization;

namespace OpenPlanningPoker.UserManagement.GraphQL.Tests.Users;

/// <summary>Error messages follow the request's UI culture (OPP-24).</summary>
public class LocalizedErrorTests
{
    private const string Secret = "e65fb783-3952-411c-b41c-1d5dc1d9ef6c--e65fb783-3952-411c-b41c-1d5dc1d9ef6c";

    [Theory]
    [InlineData("en", "The username must be between 3 and 26 characters.")]
    [InlineData("sr-Latn", "Korisničko ime mora imati od 3 do 26 znakova.")]
    [InlineData("de", "Der Benutzername muss 3 bis 26 Zeichen lang sein.")]
    public async Task RegisterUser_TooLong_MessageInRequestLanguage(string culture, string expected)
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Authentication:Secret"] = Secret;
        var executor = await new ServiceCollection()
            .AddSingleton(Substitute.For<IUsernameGeneratorService>())
            .AddSingleton(Substitute.For<IUserService>())
            .AddSingleton(configuration)
            .AddSingleton<ActionRateLimiter>()
            .AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>()
            .AddSingleton(new JwtSettings(Secret, "open-planning-poker", "open-planning-poker", TimeSpan.FromDays(1)))
            .AddLogging()
            .AddGraphQL()
            .AddAuthorization()
            .AddQueryConventions()
            .AddMutationConventions()
            .TryAddTypeInterceptor<ApplicationErrorMessageInterceptor>()
            .AddTypes()
            .BuildRequestExecutorAsync();

        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            var result = await executor.ExecuteAsync(
                """mutation { registerUser(input: { username: "Username123Username123Username123" }) { errors { ... on ApplicationError { code message } } } }""");
            var json = result.ToJson();
            Assert.Contains("MaximumLengthValidator", json);
            Assert.Contains(expected, json);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void UnknownCode_KeepsItsMessage() =>
        Assert.Equal("Something else", ServerText.ErrorMessage("Other", "Something else"));
}
