namespace OpenPlanningPoker.Shared.Identity;

public interface ICurrentUserProvider
{
    Guid Id { get; }
    Task<BaseUserProfile> GetAsync(CancellationToken cancellationToken);
}