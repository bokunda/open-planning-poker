using OpenPlanningPoker.GameEngine.Application.Features.Teams;
using OpenPlanningPoker.GameEngine.Domain.Teams;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Validation;

/// <summary>The suggested point budget of the sprint plan (OPP-147).</summary>
public class SprintBudgetTests
{
    [Fact]
    public void WithLessThanTwoSprints_ThereIsNoSuggestion()
    {
        var budget = Planning.Budget([new Planning.PastSprint(30, null)], 40);

        budget.EnoughHistory.ShouldBeFalse();
        budget.Suggested.ShouldBeNull();
    }

    [Fact]
    public void WithCapacity_UsesPointsPerPersonDay_OfTheLastThreeSprints()
    {
        // 30 pts / 40 d, 20 pts / 20 d, 45 pts / 45 d; a fourth, older sprint is ignored.
        var past = new List<Planning.PastSprint>
        {
            new(30, 40), new(20, 20), new(45, 45), new(500, 10),
        };

        var budget = Planning.Budget(past, 30);

        budget.BasedOnCapacity.ShouldBeTrue();
        budget.SprintsUsed.ShouldBe(3);
        budget.PointsPerPersonDay.ShouldBe(0.9); // 95 / 105
        budget.Suggested.ShouldBe(27);
        budget.Low.ShouldBe(22);  // 0.75 × 30
        budget.High.ShouldBe(30); // 1.0 × 30
    }

    [Fact]
    public void WithoutCapacity_UsesTheAveragePoints()
    {
        var budget = Planning.Budget([new(30, null), new(21, null), new(40, null)], 25);

        budget.BasedOnCapacity.ShouldBeFalse();
        budget.Suggested.ShouldBe(30);
        budget.Low.ShouldBe(21);
        budget.High.ShouldBe(40);
    }

    [Fact]
    public void PersonDays_UseTheSprintLength_ForMembersWithoutDays()
    {
        var game = Guid.NewGuid();
        var ana = Guid.NewGuid();
        var ben = Guid.NewGuid();
        var cap = SprintCapacity.Create(game, ana, 6);

        Planning.PersonDays(10, [ana, ben], [cap]).ShouldBe(16);
        // Days above a shorter sprint are capped.
        Planning.PersonDays(5, [ana, ben], [cap]).ShouldBe(10);
    }

    [Fact]
    public void Points_AreNumericEstimatesOnly()
    {
        Planning.PointsOf("8").ShouldBe(8);
        Planning.PointsOf("0.5").ShouldBe(0.5);
        Planning.PointsOf("XL").ShouldBeNull();
        Planning.PointsOf(null).ShouldBeNull();
    }
}
