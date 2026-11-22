using OpenPlanningPoker.GameEngine.GraphQL.Features.Info;

namespace OpenPlanningPoker.GameEngine.GraphQL.Tests;

/// <summary>Only page names are counted (OPP-181): anything that could carry personal data is dropped.</summary>
public class SiteMetricsTests
{
    [Theory]
    [InlineData("velocity-calculator", "velocity-calculator")]
    [InlineData("home", "home")]
    [InlineData("Velocity", null)]
    [InlineData("a@b.com", null)]
    [InlineData("../etc", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Page_AcceptsOnlyPageNames(string? value, string? expected) => Assert.Equal(expected, SiteMetrics.Page(value));

    [Fact]
    public void Page_RejectsLongValues() => Assert.Null(SiteMetrics.Page(new string('a', 41)));
}
