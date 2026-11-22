using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Info;

/// <summary>
/// Which website pages bring rooms (OPP-181). Two counters, exported to Prometheus through the OpenTelemetry collector:
/// visits per website page and rooms created per page the visitor came from. Only the page name is counted: no
/// cookie, no IP address, no user id.
/// </summary>
public static partial class SiteMetrics
{
    public const string MeterName = "OpenPlanningPoker.Site";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Visits = Meter.CreateCounter<long>("opp_site_visits", description: "Website page views by page.");
    private static readonly Counter<long> Rooms = Meter.CreateCounter<long>("opp_rooms_created", description: "Rooms created, by the website page the visitor came from.");

    [GeneratedRegex("^[a-z0-9-]{1,40}$")]
    private static partial Regex PageName();

    /// <summary>A page name from a link, or null when it is not one (anything else is not counted).</summary>
    public static string? Page(string? value) => value is not null && PageName().IsMatch(value) ? value : null;

    public static void Visit(string page) => Visits.Add(1, new KeyValuePair<string, object?>("page", page));

    public static void RoomCreated(string? source) => Rooms.Add(1, new KeyValuePair<string, object?>("source", Page(source) ?? "app"));

    /// <summary><c>POST /site/visit?page=…</c>, sent by the website with <c>navigator.sendBeacon</c>.</summary>
    public static void MapSiteMetrics(this IEndpointRouteBuilder app) =>
        app.MapPost("/site/visit", (string? page) =>
        {
            if (Page(page) is { } name) Visit(name);
            return Results.NoContent();
        }).AllowAnonymous();
}
