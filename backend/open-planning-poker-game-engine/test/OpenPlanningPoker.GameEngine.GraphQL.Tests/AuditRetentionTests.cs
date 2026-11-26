using OpenPlanningPoker.GameEngine.GraphQL.Features.Audits;

namespace OpenPlanningPoker.GameEngine.GraphQL.Tests;

/// <summary>How long the activity log is kept (GitHub #20).</summary>
public class AuditRetentionTests
{
    [Fact]
    public void Cutoff_IsTheRetentionBeforeNow_AndAtLeastADay()
    {
        var now = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(now.AddDays(-90), AuditRetentionService.Cutoff(now, 90));
        Assert.Equal(now.AddDays(-1), AuditRetentionService.Cutoff(now, 0));
    }
}
