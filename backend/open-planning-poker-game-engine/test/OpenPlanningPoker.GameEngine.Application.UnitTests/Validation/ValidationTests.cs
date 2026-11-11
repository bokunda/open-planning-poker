using MediatR;
using OpenPlanningPoker.GameEngine.Application.Behaviors;
using OpenPlanningPoker.GameEngine.Application.Features.Games;
using OpenPlanningPoker.GameEngine.Application.Features.GameSettings;
using OpenPlanningPoker.Shared.Errors;
using OpenPlanningPoker.Shared.Results;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Validation;

public class DeckValidationTests
{
    [Theory]
    [InlineData(GameSettingsConstants.FibonacciDeckSetup)]
    [InlineData("XS,S,M,L,XL")]
    [InlineData("1, 2, 4, 8")]
    public void IsValidDeck_AcceptsValidDecks(string deck) =>
        GameSettingsConstants.IsValidDeck(deck).ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1,,2")]
    [InlineData("1,2,2")]
    [InlineData("1,this-card-is-too-long")]
    public void IsValidDeck_RejectsInvalidDecks(string? deck) =>
        GameSettingsConstants.IsValidDeck(deck).ShouldBeFalse();

    [Fact]
    public void IsValidDeck_RejectsTooManyCards() =>
        GameSettingsConstants.IsValidDeck(string.Join(",", Enumerable.Range(1, GameSettingsConstants.MaxDeckCards + 1)))
            .ShouldBeFalse();
}

public class ValidationBehaviorTests
{
    private static readonly ValidationBehavior<CreateGameCommand, Result<CreateGameResponse, ApplicationError>> Behavior =
        new([new CreateGame.Validator()]);

    [Fact]
    public async Task InvalidRequest_ReturnsValidationError_WithoutCallingHandler()
    {
        var handlerCalled = false;
        RequestHandlerDelegate<Result<CreateGameResponse, ApplicationError>> next = () =>
        {
            handlerCalled = true;
            return Task.FromResult<Result<CreateGameResponse, ApplicationError>>(new CreateGameResponse(Guid.NewGuid(), "x", "", Guid.NewGuid()));
        };

        var result = await Behavior.Handle(new CreateGameCommand(new string('x', 256), ""), next, CancellationToken.None);

        handlerCalled.ShouldBeFalse();
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("ValidationFailed");
    }

    [Fact]
    public async Task ValidRequest_CallsHandler()
    {
        var response = new CreateGameResponse(Guid.NewGuid(), "Sprint 42", "", Guid.NewGuid());
        RequestHandlerDelegate<Result<CreateGameResponse, ApplicationError>> next =
            () => Task.FromResult<Result<CreateGameResponse, ApplicationError>>(response);

        var result = await Behavior.Handle(new CreateGameCommand("Sprint 42", ""), next, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(response);
    }
}
