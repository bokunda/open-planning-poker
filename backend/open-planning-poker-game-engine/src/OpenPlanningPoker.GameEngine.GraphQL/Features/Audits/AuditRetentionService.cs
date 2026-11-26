using OpenPlanningPoker.GameEngine.Domain.Audits;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Audits;

/// <summary>
/// Deletes activity log entries older than <c>Audit:RetentionDays</c> (default 90), once shortly after start-up and
/// then every 24 hours (GitHub #20).
/// </summary>
public sealed class AuditRetentionService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<AuditRetentionService> logger)
    : BackgroundService
{
    public const int DefaultRetentionDays = 90;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public static DateTimeOffset Cutoff(DateTimeOffset now, int retentionDays) =>
        now.AddDays(-Math.Max(1, retentionDays));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the migrations and the first requests go first.
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var audits = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
                var days = configuration.GetValue("Audit:RetentionDays", DefaultRetentionDays);
                var deleted = await audits.DeleteOlderThanAsync(Cutoff(DateTimeOffset.UtcNow, days), stoppingToken);
                if (deleted > 0)
                {
                    logger.LogInformation("Deleted {Count} activity log entries older than {Days} days", deleted, days);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Activity log retention failed; trying again in {Interval}", Interval);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
