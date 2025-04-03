namespace OpenPlanningPoker.GameEngine.Application.UnitTests.QueryableExtensionsTests;

public class GamePlayerQueryableExtensionsTests
{
    private readonly Guid _firstGame = Guid.Parse("2c6ed343-82b5-4002-a842-8fc6d2660392");
    private readonly Guid _secondGame = Guid.Parse("5e8e4110-e17c-4df7-9843-c250a0c2ed6a");

    private readonly Guid _firstPlayer = Guid.Parse("778078f0-99f1-4150-9ed7-38a92967994b");
    private readonly Guid _secondPlayer = Guid.Parse("6d579416-68d2-47a9-bd7b-fb85dbd994c9");

    private readonly ICollection<GamePlayer> _gamePlayers;

    public GamePlayerQueryableExtensionsTests()
    {
        _gamePlayers = new List<GamePlayer>
        {
            GamePlayer.Create(_firstGame, _firstPlayer),
            GamePlayer.Create(_secondGame, _secondPlayer)
        };
    }

    [Fact]
    public void GetGamePlayerByGame()
    {
        // Arrange
        var queryable = _gamePlayers.AsQueryable();

        // Act
        var result = queryable.QueryByGame(_firstGame).Single();

        // Assert
        result.GameId.ShouldBe(_firstGame);
        result.PlayerId.ShouldBe(_firstPlayer);
    }

    [Fact]
    public void GetGamePlayerByPlayer()
    {
        // Arrange
        var queryable = _gamePlayers.AsQueryable();

        // Act
        var result = queryable.QueryByPlayer(_firstPlayer).ToList();

        // Assert
        result.Count.ShouldBe(1);
    }

}