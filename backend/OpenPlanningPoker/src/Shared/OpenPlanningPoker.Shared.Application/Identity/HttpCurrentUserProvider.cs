namespace OpenPlanningPoker.Shared.Application.Identity;

public class HttpCurrentUserProvider(
    IHttpContextAccessor httpContextAccessor,
    HybridCache cache) : ICurrentUserProvider
{
    private const string UserIdHeaderName = "X-UserId";
    public Guid UserId =>
        httpContextAccessor.HttpContext?.Request.Headers[UserIdHeaderName].FirstOrDefault() is { } userId
            ? Guid.Parse(userId)
            : Guid.Empty;

    public async Task<BaseUserProfile> GetUserAsync(CancellationToken cancellationToken)
    {
        var user = await cache.GetOrCreateAsync(
            GetCacheKey(UserId),
            async cancellationToken => await Task.FromResult(new BaseUserProfile(Guid.Empty, string.Empty)),
            cancellationToken: cancellationToken);

        if (user?.Id == Guid.Empty || string.IsNullOrEmpty(user?.UserName))
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        return user;
    }

    private static string GetCacheKey(Guid userId) => $"User:{userId}";
}