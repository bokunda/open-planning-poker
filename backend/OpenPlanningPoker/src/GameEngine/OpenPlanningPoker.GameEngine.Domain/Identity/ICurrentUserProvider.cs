namespace OpenPlanningPoker.GameEngine.Domain.Identity;

public interface ICurrentUserProvider
{
    Guid CustomerId { get; }
}