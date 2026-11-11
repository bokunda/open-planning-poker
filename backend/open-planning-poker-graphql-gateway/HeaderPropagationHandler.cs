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
                var token = authHeader.ToString();
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token["Bearer ".Length..];
                }
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            // Subgraphs rate-limit anonymous actions (e.g. registration) by the real client address.
            request.Headers.Remove(ClientRateLimiting.ClientIpHeader);
            request.Headers.TryAddWithoutValidation(ClientRateLimiting.ClientIpHeader, ClientRateLimiting.GetClientIp(httpContext));
        }

        return await base.SendAsync(request, cancellationToken);
    }
}