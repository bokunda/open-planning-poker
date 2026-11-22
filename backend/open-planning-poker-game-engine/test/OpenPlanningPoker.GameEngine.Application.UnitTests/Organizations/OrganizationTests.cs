using OpenPlanningPoker.GameEngine.Application.Features.Teams;
using OpenPlanningPoker.GameEngine.Domain.Teams;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Organizations;

public class OrganizationTests
{
    private static SprintSummary Sprint(double points, double? consensus, double? reVotes, int estimated = 3, int daysAgo = 0) =>
        new(Guid.NewGuid(), "Sprint", "", DateTimeOffset.UtcNow.AddDays(-daysAgo), 4, estimated, points,
            consensus is null ? null : new SessionStats(10, (int)(consensus * 10), (int)((reVotes ?? 0) * 10), 0, 0, 0));

    [Fact]
    public void Compare_AveragesTheTeamNumbers_VelocityOldestFirst()
    {
        var newestFirst = new[] { Sprint(21, 0.6, 0.2, daysAgo: 1), Sprint(13, 0.4, 0.4, daysAgo: 15), Sprint(0, null, null, estimated: 0, daysAgo: 30) };
        var c = OrganizationService.Compare(Guid.NewGuid(), "Payments", newestFirst);

        c.Sprints.ShouldBe(3);
        c.FirstRoundConsensusShare.ShouldBe(0.5);
        c.ReVotesPerTicket.ShouldBe(0.3);
        c.AverageVelocity.ShouldBe(17);
        c.Velocity.ShouldBe([13d, 21d]);
    }

    [Fact]
    public void Compare_WithoutSessions_HasNoNumbers()
    {
        var c = OrganizationService.Compare(Guid.NewGuid(), "New team", []);
        c.FirstRoundConsensusShare.ShouldBeNull();
        c.AverageVelocity.ShouldBeNull();
        c.Velocity.ShouldBeEmpty();
    }

    [Fact]
    public void InviteTokens_AreRandomAndUrlSafe()
    {
        var a = OrganizationService.NewInviteToken();
        a.Length.ShouldBe(22);
        a.ShouldNotContain("+");
        OrganizationService.NewInviteToken().ShouldNotBe(a);
    }

    [Fact]
    public void Team_MovesInAndOut()
    {
        var team = Team.Create("Payments", "");
        var org = Guid.NewGuid();
        team.MoveToOrganization(org);
        team.OrganizationId.ShouldBe(org);
        team.MoveToOrganization(null);
        team.OrganizationId.ShouldBeNull();
    }
}
