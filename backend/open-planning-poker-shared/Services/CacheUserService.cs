namespace OpenPlanningPoker.Shared.Services;

public class CacheUserService(HybridCache cache) : IUserService
{
    public async Task<BaseUserProfile?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        await cache.GetOrCreateAsync(
            $"User:{id}",
            async cancellationToken => await Task.FromResult<BaseUserProfile?>(null),
            cancellationToken: cancellationToken);

    public async Task<BaseUserProfile> AddAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = new BaseUserProfile(Guid.NewGuid(), username);
        await cache.SetAsync(user.GetCacheKey(), user, cancellationToken: cancellationToken);
        return user;
    }

    public async Task<BaseUserProfile?> UpdateAsync(string id, string newUserName, CancellationToken cancellationToken = default)
    {
        var user = await GetAsync(id, cancellationToken);

        if (user is null) 
        { 
            return null; 
        }

        user.UserName = newUserName;
        await cache.SetAsync(user.GetCacheKey(), user, cancellationToken: cancellationToken);

        return user;
    }
}
