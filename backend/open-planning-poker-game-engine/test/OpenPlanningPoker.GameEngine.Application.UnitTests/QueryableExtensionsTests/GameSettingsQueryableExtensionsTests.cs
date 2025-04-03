namespace OpenPlanningPoker.GameEngine.Application.UnitTests.QueryableExtensionsTests;

public class GameSettingsQueryableExtensionsTests
{
    private readonly Guid _firstGame = Guid.Parse("1c6ed343-82b5-4002-a842-8fc6d2660392");
    private readonly Guid _secondGame = Guid.Parse("2c6ed343-82b5-4002-a842-8fc6d2660392");
    private readonly Guid _thirdGame = Guid.Parse("3c6ed343-82b5-4002-a842-8fc6d2660392");


    private readonly ICollection<GameSettings> _gameSettings;

    public GameSettingsQueryableExtensionsTests()
    {
        const string DeckSetup = "0,1,2,3,4,5,6,7";
        _gameSettings = new List<GameSettings>
        {
            GameSettings.Create(_firstGame, DeckSetup),
            GameSettings.Create(_secondGame, DeckSetup),
            GameSettings.Create(_thirdGame, DeckSetup),
        };
    }

    [Fact]
    public void GetGamePlayerByGame()
    {
        // Arrange
        var queryable = _gameSettings.AsQueryable();

        // Act
        var result = queryable.QueryByGame(_firstGame).Single();

        // Assert
        result.GameId.ShouldBe(_firstGame);
    }
}