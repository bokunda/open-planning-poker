namespace OpenPlanningPoker.GameEngine.Application.UnitTests.QueryableExtensionsTests;

public class TicketQueryableExtensionsTests
{
    private readonly Guid _firstGame = Guid.Parse("2c6ed343-82b5-4002-a842-8fc6d2660392");
    private readonly Guid _secondGame = Guid.Parse("5e8e4110-e17c-4df7-9843-c250a0c2ed6a");

    private const string FirstGameName = "First Game Description";
    private const string SecondGameName = "Second Game Description";

    private const string FirstGameDescription = "First Game Description";
    private const string SecondGameDescription = "Second Game Description";


    private readonly ICollection<Ticket> _tickets;

    public TicketQueryableExtensionsTests()
    {
        _tickets = new List<Ticket>
        {
            Ticket.Create(_firstGame, FirstGameName, FirstGameDescription),
            Ticket.Create(_secondGame, SecondGameName, SecondGameDescription)
        };
    }

    [Fact]
    public void GetTicketByGame()
    {
        // Arrange
        var queryable = _tickets.AsQueryable();

        // Act
        var result = queryable.QueryByGame(_firstGame).Single();

        // Assert
        result.GameId.ShouldBe(_firstGame);
        result.Name.ShouldBe(FirstGameName);
        result.Description.ShouldBe(FirstGameDescription);
    }
}