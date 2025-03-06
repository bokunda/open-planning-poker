namespace OpenPlanningPoker.Fusion.Gateway;
public class HeaderPropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Propagate  headers
            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authHeader.ToString().Split(" ").Last());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}