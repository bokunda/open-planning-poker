namespace OpenPlanningPoker.Shared.Identity;

public interface ICurrentUserProvider
{
    Guid Id { get; }
    Task<Result<BaseUserProfile, ApplicationError>> GetAsync(CancellationToken cancellationToken);
}