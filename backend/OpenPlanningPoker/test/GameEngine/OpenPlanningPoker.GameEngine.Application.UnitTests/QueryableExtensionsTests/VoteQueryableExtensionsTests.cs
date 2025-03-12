namespace OpenPlanningPoker.GameEngine.Application.UnitTests.QueryableExtensionsTests;

public class VoteQueryableExtensionsTests
{
    private readonly Guid _firstPlayer = Guid.Parse("2c6ed343-82b5-4002-a842-8fc6d2660392");
    private readonly Guid _secondPlayer = Guid.Parse("5e8e4110-e17c-4df7-9843-c250a0c2ed6a");

    private readonly Guid _firstTicket = Guid.Parse("2c6ed343-1111-4002-a842-8fc6d2660392");

    private const string FirstValue = "10";
    private const string SecondValue = "20";

    private readonly ICollection<Vote> _votes;

    public VoteQueryableExtensionsTests()
    {
        _votes = new List<Vote>
        {
            Vote.Create(_firstPlayer, _firstTicket, FirstValue),
            Vote.Create(_secondPlayer, _firstTicket, SecondValue)
        };
    }

    [Fact]
    public void GetVotesByTicket()
    {
        // Arrange
        var queryable = _votes.AsQueryable();

        // Act
        var result = queryable.QueryByTicket(_firstTicket).ToList();

        // Assert
        result.Count.ShouldBe(_votes.Count);
    }
}