using System.Text.Json;

namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>Shareable recaps and the room look (OPP-169) through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class RecapTests(GameEngineFactory factory)
{
    private const string Errors = "errors { ... on ApplicationError { code } }";
    private const string Sharing = "shared token showNames";

    private static readonly string Share =
        $"mutation($input: SetRecapSharingInput!) {{ setRecapSharing(input: $input) {{ recapSharing {{ {Sharing} }} {Errors} }} }}";
    private const string Recap =
        "query($token: String!) { gameRecap(token: $token) { ... on GameRecap { gameName ticketsEstimated ticketsOpen totalPoints consensusRate playerCount players tickets { name externalKey estimate spread voteRounds } } ... on ApplicationError { code } } }";
    private const string SetFinal =
        "mutation($input: SetFinalEstimateInput!) { setFinalEstimate(input: $input) { ticket { id } errors { ... on ApplicationError { code } } } }";
    private static readonly string Options =
        $"mutation($input: UpdateFacilitationOptionsInput!) {{ updateFacilitationOptions(input: $input) {{ facilitationSettings {{ reactions celebrations theme seasonalCards }} {Errors} }} }}";
    private static readonly string Avatar =
        $"mutation($input: SetPlayerAvatarInput!) {{ setPlayerAvatar(input: $input) {{ boolean {Errors} }} }}";

    private static Guid Id(JsonElement created) => created.GetProperty("ticket").GetProperty("id").GetGuid();

    private async Task<(GraphQLClient Host, GraphQLClient Player, Guid GameId)> PlayedGameAsync()
    {
        var host = await factory.CreateUserAsync("Ana");
        var player = await factory.CreateUserAsync("Marko");
        var gameId = await host.CreateGameAsync("Sprint 7 planning");
        await host.JoinAsync(gameId);
        await player.JoinAsync(gameId);

        var agreed = Id(await host.CreateTicketAsync(gameId, "Login page"));
        await host.VoteAsync(agreed, "5");
        await player.VoteAsync(agreed, "5");
        await host.RevealAsync(agreed);

        var split = Id(await host.CreateTicketAsync(gameId, "Export to CSV"));
        await host.VoteAsync(split, "3");
        await player.VoteAsync(split, "13");
        await host.RevealAsync(split);
        await host.RunAsync(SetFinal, new { input = new { ticketId = split, value = "8" } });

        var open = Id(await host.CreateTicketAsync(gameId, "Secret roadmap item"));
        await host.VoteAsync(open, "21");
        return (host, player, gameId);
    }

    [Fact]
    public async Task Recap_IsSharedByTheHost_ReadableWithoutSignIn_AndStopsWorkingWhenUnshared()
    {
        var (host, player, gameId) = await PlayedGameAsync();

        var byPlayer = await player.RunAsync(Share, new { input = new { gameId, shared = true } });
        GraphQLClient.ErrorCode(byPlayer.GetProperty("setRecapSharing")).ShouldBe("NotHost");

        var shared = (await host.RunAsync(Share, new { input = new { gameId, shared = true } }))
            .GetProperty("setRecapSharing").GetProperty("recapSharing");
        shared.GetProperty("shared").GetBoolean().ShouldBeTrue();
        var token = shared.GetProperty("token").GetString()!;
        token.Length.ShouldBe(22);

        var anonymous = factory.CreateAnonymous();
        var recap = (await anonymous.RunAsync(Recap, new { token })).GetProperty("gameRecap");
        recap.GetProperty("gameName").GetString().ShouldBe("Sprint 7 planning");
        recap.GetProperty("ticketsEstimated").GetInt32().ShouldBe(2);
        recap.GetProperty("ticketsOpen").GetInt32().ShouldBe(1);
        recap.GetProperty("totalPoints").GetDouble().ShouldBe(13);
        recap.GetProperty("consensusRate").GetDouble().ShouldBe(0.5);
        recap.GetProperty("playerCount").GetInt32().ShouldBe(2);
        recap.GetProperty("players").GetArrayLength().ShouldBe(0);
        var names = recap.GetProperty("tickets").EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToList();
        names.ShouldBe(["Login page", "Export to CSV"]);
        recap.GetProperty("tickets")[0].GetProperty("spread").GetString().ShouldBe("CONSENSUS");
        recap.GetProperty("tickets")[1].GetProperty("estimate").GetString().ShouldBe("8");

        // Sharing again keeps the link and can show names.
        var withNames = (await host.RunAsync(Share, new { input = new { gameId, shared = true, showNames = true } }))
            .GetProperty("setRecapSharing").GetProperty("recapSharing");
        withNames.GetProperty("token").GetString().ShouldBe(token);
        var named = (await anonymous.RunAsync(Recap, new { token })).GetProperty("gameRecap");
        named.GetProperty("players").EnumerateArray().Select(p => p.GetString()).ShouldBe(["Ana", "Marko"]);

        await host.RunAsync(Share, new { input = new { gameId, shared = false } });
        var gone = (await anonymous.RunAsync(Recap, new { token })).GetProperty("gameRecap");
        gone.GetProperty("code").GetString().ShouldBe("RecapNotFound");

        var malformed = (await anonymous.RunAsync(Recap, new { token = "not-a-token" })).GetProperty("gameRecap");
        malformed.GetProperty("code").GetString().ShouldBe("RecapNotFound");
    }

    [Fact]
    public async Task RecapCard_IsAPng_AndNotFoundOnceUnshared()
    {
        var (host, _, gameId) = await PlayedGameAsync();
        var token = (await host.RunAsync(Share, new { input = new { gameId, shared = true } }))
            .GetProperty("setRecapSharing").GetProperty("recapSharing").GetProperty("token").GetString()!;

        using var http = factory.CreateClient();
        var card = await http.GetAsync($"/recap/{token}/card.png");
        card.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        card.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        var bytes = await card.Content.ReadAsByteArrayAsync();
        bytes.Take(4).ShouldBe(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        await host.RunAsync(Share, new { input = new { gameId, shared = false } });
        (await http.GetAsync($"/recap/{token}/card.png")).StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
        (await http.GetAsync("/recap/short/card.png")).StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RoomLook_IsKeptWhenLeftOut_AndThemesAndAvatarsAreChecked()
    {
        var host = await factory.CreateUserAsync("Ana");
        var player = await factory.CreateUserAsync("Marko");
        var gameId = await host.CreateGameAsync();
        await host.JoinAsync(gameId);
        await player.JoinAsync(gameId);

        var themed = (await host.RunAsync(Options, new { input = new { gameId, outlierNotes = true, discussionMinutes = 0, confidenceCheck = false, reactions = true, celebrations = false, theme = "ocean" } }))
            .GetProperty("updateFacilitationOptions").GetProperty("facilitationSettings");
        themed.GetProperty("theme").GetString().ShouldBe("ocean");
        themed.GetProperty("celebrations").GetBoolean().ShouldBeFalse();
        themed.GetProperty("seasonalCards").GetBoolean().ShouldBeTrue();

        // An older client that does not send the look keeps it.
        var kept = (await host.RunAsync(Options, new { input = new { gameId, outlierNotes = true, discussionMinutes = 0, confidenceCheck = false, reactions = false } }))
            .GetProperty("updateFacilitationOptions").GetProperty("facilitationSettings");
        kept.GetProperty("theme").GetString().ShouldBe("ocean");
        kept.GetProperty("celebrations").GetBoolean().ShouldBeFalse();

        var badTheme = await host.RunAsync(Options, new { input = new { gameId, outlierNotes = true, discussionMinutes = 0, confidenceCheck = false, reactions = true, theme = "neon" } });
        GraphQLClient.ErrorCode(badTheme.GetProperty("updateFacilitationOptions")).ShouldBe("UnknownTheme");

        var badAvatar = await player.RunAsync(Avatar, new { input = new { gameId, avatar = "💩" } });
        GraphQLClient.ErrorCode(badAvatar.GetProperty("setPlayerAvatar")).ShouldBe("UnknownAvatar");
        var ok = await player.RunAsync(Avatar, new { input = new { gameId, avatar = "🦊" } });
        GraphQLClient.ErrorCode(ok.GetProperty("setPlayerAvatar")).ShouldBeNull();

        var players = await host.RunAsync("query($gameId: UUID!) { gamePlayers(gameId: $gameId) { ... on ApiCollectionOfGamePlayer { items { id avatar } } } }", new { gameId });
        players.GetProperty("gamePlayers").GetProperty("items").EnumerateArray()
            .Single(p => p.GetProperty("id").GetGuid() == player.UserId).GetProperty("avatar").GetString().ShouldBe("🦊");
    }
}
