namespace OpenPlanningPoker.Shared.Identity;

public class HttpCurrentUserProvider(
    IHttpContextAccessor httpContextAccessor,
    HybridCache cache) : ICurrentUserProvider
{
    private const string UserIdKey = "id";
    public Guid Id =>
        httpContextAccessor.HttpContext?.User.FindFirst(UserIdKey)?.Value is { } userId
            ? Guid.Parse(userId)
            : Guid.Empty;

    public async Task<Result<BaseUserProfile, ApplicationError>> GetAsync(CancellationToken cancellationToken)
    {
        var user = await cache.GetOrCreateAsync(
            GetCacheKey(Id),
            async cancellationToken => await Task.FromResult(new BaseUserProfile(Guid.Empty, string.Empty)),
            cancellationToken: cancellationToken);

        if (user?.Id == Guid.Empty || string.IsNullOrEmpty(user?.UserName))
        {
            return new ApplicationError(
                ApplicationErrorContants.UserNotFound.Key, 
                ApplicationErrorContants.UserNotFound.Value);
        }

        return user;
    }

    private static string GetCacheKey(Guid userId) => $"User:{userId}";
}