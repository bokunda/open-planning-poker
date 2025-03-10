namespace OpenPlanningPoker.Shared.Services;

public interface IUserService
{
    Task<BaseUserProfile?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<BaseUserProfile> AddAsync(string username, CancellationToken cancellationToken = default);
    Task<BaseUserProfile?> UpdateAsync(string id, string newUsername, CancellationToken cancellationToken = default);
}
