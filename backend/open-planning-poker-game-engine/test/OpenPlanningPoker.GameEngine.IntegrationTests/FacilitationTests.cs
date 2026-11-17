namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>Running the discussion after the reveal (OPP-114) through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class FacilitationTests(GameEngineFactory factory)
{
    private const string Info = "ticketFacilitationInfo { revealedAt voteRounds needsInfo notes { playerId note } confidenceAverage confidenceCount myConfidence }";
    private const string Errors = "errors { ... on ApplicationError { code } }";

    private static readonly string SetNote =
        $"mutation($input: SetVoteNoteInput!) {{ setVoteNote(input: $input) {{ {Info} {Errors} }} }}";
    private static readonly string SetConfidence =
        $"mutation($input: SetConfidenceInput!) {{ setConfidence(input: $input) {{ {Info} {Errors} }} }}";
    private static readonly string SetNeedsInfo =
        $"mutation($input: SetTicketNeedsInfoInput!) {{ setTicketNeedsInfo(input: $input) {{ {Info} {Errors} }} }}";
    private const string SetFinal =
        "mutation($input: SetFinalEstimateInput!) { setFinalEstimate(input: $input) { ticket { id } errors { ... on ApplicationError { code } } } }";
    private const string SendReaction =
        "mutation($input: SendReactionInput!) { sendReaction(input: $input) { boolean errors { ... on ApplicationError { code } } } }";
    private const string Options =
        "mutation($input: UpdateFacilitationOptionsInput!) { updateFacilitationOptions(input: $input) { facilitationSettings { discussionMinutes confidenceCheck } errors { ... on ApplicationError { code } } } }";

    private async Task<(GraphQLClient Host, GraphQLClient Player, Guid GameId, Guid TicketId)> RevealedWideSpreadAsync(bool reveal = true)
    {
        var host = await factory.CreateUserAsync("Ana");
        var player = await factory.CreateUserAsync("Marko");
        var gameId = await host.CreateGameAsync();
        await host.JoinAsync(gameId);
        await player.JoinAsync(gameId);
        var ticketId = (await host.CreateTicketAsync(gameId)).GetProperty("ticket").GetProperty("id").GetGuid();
        await host.VoteAsync(ticketId, "1");
        await player.VoteAsync(ticketId, "13");
        if (reveal) await host.RevealAsync(ticketId);
        return (host, player, gameId, ticketId);
    }

    [Fact]
    public async Task Notes_OnlyAfterTheReveal_AndVisibleToEveryone()
    {
        var (host, player, _, ticketId) = await RevealedWideSpreadAsync(reveal: false);

        var early = await player.RunAsync(SetNote, new { input = new { ticketId, note = "Needs a migration" } });
        GraphQLClient.ErrorCode(early.GetProperty("setVoteNote")).ShouldBe("CardsNotRevealed");

        await host.RevealAsync(ticketId);
        var saved = await player.RunAsync(SetNote, new { input = new { ticketId, note = "  Needs a migration  " } });
        var info = saved.GetProperty("setVoteNote").GetProperty("ticketFacilitationInfo");
        info.GetProperty("voteRounds").GetInt32().ShouldBe(1);
        info.GetProperty("revealedAt").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.String);

        var seen = await host.RunAsync("query($ticketId: UUID!) { ticketFacilitation(ticketId: $ticketId) { ... on TicketFacilitationInfo { notes { playerId note } } } }", new { ticketId });
        var note = seen.GetProperty("ticketFacilitation").GetProperty("notes")[0];
        note.GetProperty("playerId").GetGuid().ShouldBe(player.UserId);
        note.GetProperty("note").GetString().ShouldBe("Needs a migration");
    }

    [Fact]
    public async Task Confidence_NeedsTheFinalEstimate_AndIsAveraged()
    {
        var (host, player, _, ticketId) = await RevealedWideSpreadAsync();

        var early = await player.RunAsync(SetConfidence, new { input = new { ticketId, value = 4 } });
        GraphQLClient.ErrorCode(early.GetProperty("setConfidence")).ShouldBe("NoFinalEstimate");

        await host.RunAsync(SetFinal, new { input = new { ticketId, value = "8" } });
        var invalid = await player.RunAsync(SetConfidence, new { input = new { ticketId, value = 7 } });
        GraphQLClient.ErrorCode(invalid.GetProperty("setConfidence")).ShouldBe("InvalidConfidence");

        await player.RunAsync(SetConfidence, new { input = new { ticketId, value = 2 } });
        var both = await host.RunAsync(SetConfidence, new { input = new { ticketId, value = 3 } });
        var info = both.GetProperty("setConfidence").GetProperty("ticketFacilitationInfo");
        info.GetProperty("confidenceCount").GetInt32().ShouldBe(2);
        info.GetProperty("confidenceAverage").GetDecimal().ShouldBe(2.5m);
        info.GetProperty("myConfidence").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task NeedsInfo_IsForTheHost_AndClearedByTheFinalEstimate()
    {
        var (host, player, _, ticketId) = await RevealedWideSpreadAsync();

        var byPlayer = await player.RunAsync(SetNeedsInfo, new { input = new { ticketId, needsInfo = true } });
        GraphQLClient.ErrorCode(byPlayer.GetProperty("setTicketNeedsInfo")).ShouldBe("NotHost");

        var marked = await host.RunAsync(SetNeedsInfo, new { input = new { ticketId, needsInfo = true } });
        marked.GetProperty("setTicketNeedsInfo").GetProperty("ticketFacilitationInfo").GetProperty("needsInfo").GetBoolean().ShouldBeTrue();

        await host.RunAsync(SetFinal, new { input = new { ticketId, value = "5" } });
        var after = await host.RunAsync(SetNeedsInfo, new { input = new { ticketId, needsInfo = false } });
        after.GetProperty("setTicketNeedsInfo").GetProperty("ticketFacilitationInfo").GetProperty("needsInfo").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Reactions_AreChecked_AndCanBeTurnedOff()
    {
        var (host, player, gameId, _) = await RevealedWideSpreadAsync();

        var unknown = await player.RunAsync(SendReaction, new { input = new { gameId, emoji = "💩" } });
        GraphQLClient.ErrorCode(unknown.GetProperty("sendReaction")).ShouldBe("InvalidReaction");

        var ok = await player.RunAsync(SendReaction, new { input = new { gameId, emoji = "👍" } });
        GraphQLClient.ErrorCode(ok.GetProperty("sendReaction")).ShouldBeNull();

        var badTime = await host.RunAsync(Options, new { input = new { gameId, outlierNotes = true, discussionMinutes = 4, confidenceCheck = true, reactions = false } });
        GraphQLClient.ErrorCode(badTime.GetProperty("updateFacilitationOptions")).ShouldBe("InvalidDiscussionTime");

        var saved = await host.RunAsync(Options, new { input = new { gameId, outlierNotes = true, discussionMinutes = 3, confidenceCheck = true, reactions = false } });
        saved.GetProperty("updateFacilitationOptions").GetProperty("facilitationSettings").GetProperty("discussionMinutes").GetInt32().ShouldBe(3);

        var off = await player.RunAsync(SendReaction, new { input = new { gameId, emoji = "👍" } });
        GraphQLClient.ErrorCode(off.GetProperty("sendReaction")).ShouldBe("ReactionsOff");
    }
}
