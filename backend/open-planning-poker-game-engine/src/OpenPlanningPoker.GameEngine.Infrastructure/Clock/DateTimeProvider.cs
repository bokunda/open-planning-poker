namespace OpenPlanningPoker.GameEngine.Infrastructure.Clock;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}