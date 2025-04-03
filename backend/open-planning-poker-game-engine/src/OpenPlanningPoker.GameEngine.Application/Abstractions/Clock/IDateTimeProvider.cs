namespace OpenPlanningPoker.GameEngine.Application.Abstractions.Clock;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}