using Microsoft.Extensions.Caching.Memory;
using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using OpenPlanningPoker.GameEngine.Application.Features.Recaps;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Recaps;

/// <summary>Whether the recap of a game is shared (OPP-170). The link is app.openplanningpoker.com/recap/{token}.</summary>
public sealed class RecapSharing
{
    public bool Shared { get; set; }
    public string? Token { get; set; }

    /// <summary>The recap lists who took part.</summary>
    public bool ShowNames { get; set; }

    public static RecapSharing From(RecapSharingResponse r) => new() { Shared = r.Shared, Token = r.Token, ShowNames = r.ShowNames };
}

public sealed class RecapTicket
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Key of the tracker issue (e.g. "OPP-118"); no link or description is shared.</summary>
    public string? ExternalKey { get; set; }
    public string? Estimate { get; set; }

    /// <summary>CONSENSUS, CLOSE, WIDE, NO_VOTES or NOT_REVEALED (estimated on the sorting board).</summary>
    public VoteSpread Spread { get; set; }
    public int VoteRounds { get; set; }

    /// <summary>Minutes from turning the cards over to choosing the estimate.</summary>
    public double? DiscussionMinutes { get; set; }
}

/// <summary>Public, read-only recap of a session: numbers and ticket titles only.</summary>
public sealed class GameRecap
{
    public string GameName { get; set; } = string.Empty;
    public DateTimeOffset PlayedOn { get; set; }
    public List<RecapTicket> Tickets { get; set; } = [];
    public int TicketsEstimated { get; set; }
    public int TicketsOpen { get; set; }
    public double TotalPoints { get; set; }

    /// <summary>Share (0–1) of tickets with at least two votes where everyone agreed; null when none.</summary>
    public double? ConsensusRate { get; set; }
    public double? DurationMinutes { get; set; }
    public int PlayerCount { get; set; }

    /// <summary>Who took part; empty unless the host chose to show names.</summary>
    public List<string> Players { get; set; } = [];

    public static GameRecap From(RecapResponse r) => new()
    {
        GameName = r.GameName,
        PlayedOn = r.PlayedOn,
        Tickets = [.. r.Tickets.Select(t => new RecapTicket
        {
            Name = t.Name,
            ExternalKey = t.ExternalKey,
            Estimate = t.Estimate,
            Spread = t.Spread,
            VoteRounds = t.VoteRounds,
            DiscussionMinutes = t.DiscussionMinutes,
        })],
        TicketsEstimated = r.TicketsEstimated,
        TicketsOpen = r.TicketsOpen,
        TotalPoints = r.TotalPoints,
        ConsensusRate = r.ConsensusRate,
        DurationMinutes = r.DurationMinutes,
        PlayerCount = r.PlayerCount,
        Players = [.. r.Players],
    };
}

[QueryType]
public class RecapQueries
{
    /// <summary>The shared recap behind a recap link. No sign-in needed; RecapNotFound once the host stops sharing.</summary>
    public async Task<FieldResult<GameRecap, ApplicationError>> GetGameRecapAsync(
        [Service] ISender sender,
        [Required] string token,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetRecapQuery(token), cancellationToken);
        return result.IsSuccess ? GameRecap.From(result.Value!) : result.Error!;
    }

    /// <summary>Whether the recap of a game is shared. Host and co-hosts.</summary>
    public async Task<FieldResult<RecapSharing, ApplicationError>> GetRecapSharingAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new GetRecapSharingQuery(gameId), cancellationToken);
        return result.IsSuccess ? RecapSharing.From(result.Value!) : result.Error!;
    }
}

[MutationType]
public class RecapMutations
{
    /// <summary>
    /// Shares the recap of a game as a read-only link, or stops sharing it (the link stops working). Sharing again
    /// keeps the same link. Host and co-hosts.
    /// </summary>
    public async Task<FieldResult<RecapSharing, ApplicationError>> SetRecapSharingAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Service] IMemoryCache cache,
        [Required] Guid gameId,
        [Required] bool shared,
        bool showNames = false,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var before = await sender.Send(new GetRecapSharingQuery(gameId), cancellationToken);
        var result = await sender.Send(new SetRecapSharingCommand(gameId, shared, showNames), cancellationToken);
        if (before.Value?.Token is { } oldToken) RecapEndpoints.Forget(cache, oldToken);
        return result.IsSuccess ? RecapSharing.From(result.Value!) : result.Error!;
    }
}

/// <summary>The link preview image of a shared recap: GET /recap/{token}/card.png (OPP-171).</summary>
public static class RecapEndpoints
{
    private static string Key(string token) => $"recap-card:{token}";

    public static void Forget(IMemoryCache cache, string token) => cache.Remove(Key(token));

    public static void MapRecapEndpoints(this WebApplication app) =>
        app.MapGet("/recap/{token}/card.png", async (string token, ISender sender, IMemoryCache cache, HttpContext ctx, CancellationToken cancellationToken) =>
        {
            if (!Recap.IsWellFormed(token)) return Results.NotFound();

            // Chat apps fetch the image once per unfurl; a short cache keeps repeated fetches cheap.
            if (!cache.TryGetValue(Key(token), out byte[]? png))
            {
                var result = await sender.Send(new GetRecapCardQuery(token), cancellationToken);
                if (!result.IsSuccess) return Results.NotFound();
                png = result.Value!;
                cache.Set(Key(token), png, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10), Size = 1 });
            }

            ctx.Response.Headers.CacheControl = "public, max-age=600";
            return Results.File(png!, "image/png");
        });
}
