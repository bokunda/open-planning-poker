namespace OpenPlanningPoker.Shared.Services;

public interface IUserService
{
    Task<BaseUserProfile?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
}
