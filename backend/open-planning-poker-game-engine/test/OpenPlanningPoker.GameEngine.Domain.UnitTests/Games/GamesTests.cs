namespace OpenPlanningPoker.GameEngine.Domain.UnitTests.Games;

public class GamesTests : BaseTest
{
    [Fact]
    public void CreateGame_Should_Raise_CreateGameDomainEvent()
    {
        // Arrange
        const string name = "Test Name";
        const string description = "Description Test";

        // Act
        var game = Game.Create(name, description);

        // Assert
        AssertDomainEventWasPublished<CreateGameDomainEvent>(game);
        game.Name.ShouldBe(name);
        game.Description.ShouldBe(description);
    }

}