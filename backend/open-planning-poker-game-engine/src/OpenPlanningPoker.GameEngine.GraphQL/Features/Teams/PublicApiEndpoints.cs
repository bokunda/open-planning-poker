using OpenPlanningPoker.GameEngine.GraphQL.RateLimiting;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Teams;

/// <summary>
/// The read-only public API of a team (OPP-159) on <c>https://ge.openplanningpoker.com/api/v1</c>:
/// <c>Authorization: Bearer opp_…</c> with a token the team owner created. JSON in, JSON out, paged with skip and take.
/// </summary>
public static class PublicApiEndpoints
{
    private const string TeamItem = "opp-api-team";

    public static void MapPublicApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1")
            .AllowAnonymous()
            .RequireRateLimiting(ClientRateLimiting.PublicApiPolicy)
            .AddEndpointFilter(async (context, next) =>
            {
                var http = context.HttpContext;
                var header = http.Request.Headers.Authorization.ToString();
                var raw = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : null;
                var token = await http.RequestServices.GetRequiredService<TeamApiService>().AuthenticateAsync(raw, http.RequestAborted);
                if (token is null)
                {
                    http.Response.Headers.WWWAuthenticate = "Bearer";
                    return Problem(StatusCodes.Status401Unauthorized, "unauthorized", "Send a valid team API token: Authorization: Bearer opp_…");
                }

                http.Items[TeamItem] = token.TeamId;
                http.Response.Headers.CacheControl = "no-store";
                return await next(context);
            });

        api.MapGet("/team", async (HttpContext http, TeamApiService service) =>
            await service.TeamAsync(TeamOf(http), http.RequestAborted) is { } team ? Results.Ok(team) : NotFound("team"));

        api.MapGet("/sessions", async (HttpContext http, TeamApiService service, int? skip, int? take) =>
            Results.Ok(await service.SessionsAsync(TeamOf(http), skip ?? 0, take ?? 50, http.RequestAborted)));

        api.MapGet("/sessions/{sessionId:guid}/tickets", async (HttpContext http, TeamApiService service, Guid sessionId, int? skip, int? take) =>
            await service.TicketsAsync(TeamOf(http), sessionId, skip ?? 0, take ?? 50, http.RequestAborted) is { } page
                ? Results.Ok(page)
                : NotFound("session"));
    }

    private static Guid TeamOf(HttpContext http) => (Guid)http.Items[TeamItem]!;

    private static IResult NotFound(string what) => Problem(StatusCodes.Status404NotFound, "not_found", $"No such {what} in this team.");

    private static IResult Problem(int status, string code, string message) =>
        Results.Json(new { error = new { code, message } }, statusCode: status);
}
