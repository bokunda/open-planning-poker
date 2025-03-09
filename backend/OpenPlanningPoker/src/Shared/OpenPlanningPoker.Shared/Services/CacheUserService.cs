namespace OpenPlanningPoker.Shared.Services;

public class CacheUserService(HybridCache cache) : IUserService
{
    public async Task<BaseUserProfile?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default) =>
        await cache.GetOrCreateAsync(
            $"User:{userId}",
            async cancellationToken => await Task.FromResult<BaseUserProfile?>(null),
            cancellationToken: cancellationToken);
}
