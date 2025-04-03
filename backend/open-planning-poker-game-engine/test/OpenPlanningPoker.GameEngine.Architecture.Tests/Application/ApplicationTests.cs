namespace OpenPlanningPoker.GameEngine.Architecture.Tests.Application;

public class ApplicationTests : BaseTest
{
    [Fact]
    public void QueryAndCommandHandler_Should_HaveNameEndingWith_CommandHandler()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IRequestHandler<>))
            .Should().HaveNameEndingWith("Handler")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Validator_Should_HaveNameEndingWith_Validator()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}