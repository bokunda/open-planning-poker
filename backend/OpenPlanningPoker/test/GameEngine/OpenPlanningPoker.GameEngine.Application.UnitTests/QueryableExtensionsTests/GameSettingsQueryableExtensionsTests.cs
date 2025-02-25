namespace OpenPlanningPoker.GameEngine.Application.UnitTests.QueryableExtensionsTests;

public class GameSettingsQueryableExtensionsTests
{
    private readonly Guid _firstGame = Guid.Parse("1c6ed343-82b5-4002-a842-8fc6d2660392");
    private readonly Guid _secondGame = Guid.Parse("2c6ed343-82b5-4002-a842-8fc6d2660392");
    private readonly Guid _thirdGame = Guid.Parse("3c6ed343-82b5-4002-a842-8fc6d2660392");


    private readonly ICollection<GameSettings> _gameSettings;

    public GameSettingsQueryableExtensionsTests()
    {
        _gameSettings = new List<GameSettings>
        {
            GameSettings.Create(_firstGame, 10, true),
            GameSettings.Create(_secondGame, 20, false),
            GameSettings.Create(_thirdGame, 30, true),
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