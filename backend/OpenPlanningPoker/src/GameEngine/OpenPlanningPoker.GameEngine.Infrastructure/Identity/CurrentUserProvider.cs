namespace OpenPlanningPoker.GameEngine.Infrastructure.Identity;

public class CurrentUserProvider : ICurrentUserProvider
{
    public Guid CustomerId { get; } = new("70d6c641-6299-4cb4-9f39-97c180115a34"); // TODO: Implement this!
}