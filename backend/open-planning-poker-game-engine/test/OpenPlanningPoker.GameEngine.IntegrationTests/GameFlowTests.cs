using System.Text.Json;

namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>The main game flows through the GraphQL API, against real PostgreSQL and Redis.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class GameFlowTests(GameEngineFactory factory)
{
    private async Task<(GraphQLClient Host, GraphQLClient Player, Guid GameId, Guid TicketId)> GameWithTicketAsync()
    {
        var host = await factory.CreateUserAsync("Ana");
        var player = await factory.CreateUserAsync("Marko");
        var gameId = await host.CreateGameAsync();
        await host.JoinAsync(gameId);
        await player.JoinAsync(gameId);
        var ticketId = (await host.CreateTicketAsync(gameId)).GetProperty("ticket").GetProperty("id").GetGuid();
        return (host, player, gameId, ticketId);
    }

    [Fact]
    public async Task Anonymous_CannotCreateAGame()
    {
        var data = await factory.CreateAnonymous().RunAsync(
            "mutation { createGame(input: { name: \"x\", description: \"\" }) { game { id } errors { ... on ApplicationError { code } } } }");

        GraphQLClient.ErrorCode(data.GetProperty("createGame")).ShouldBe("Unauthenticated");
    }

    [Fact]
    public async Task HostOnlyMutations_AreRejectedForPlayers()
    {
        var (_, player, gameId, ticketId) = await GameWithTicketAsync();

        GraphQLClient.ErrorCode(await player.CreateTicketAsync(gameId)).ShouldBe("NotHost");
        GraphQLClient.ErrorCode(await player.RevealAsync(ticketId)).ShouldBe("NotHost");

        var timer = await player.RunAsync(
            "mutation($input: StartTimerInput!) { startTimer(input: $input) { errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId, durationSeconds = 60 } });
        GraphQLClient.ErrorCode(timer.GetProperty("startTimer")).ShouldBe("NotHost");
    }

    [Fact]
    public async Task Votes_AreHiddenUntilTheReveal()
    {
        var (host, player, _, ticketId) = await GameWithTicketAsync();
        GraphQLClient.ErrorCode(await host.VoteAsync(ticketId, "3")).ShouldBeNull();
        GraphQLClient.ErrorCode(await player.VoteAsync(ticketId, "8")).ShouldBeNull();

        var beforeReveal = await host.VotesAsync(ticketId);
        beforeReveal[host.UserId].ShouldBe("3");
        beforeReveal[player.UserId].ShouldBe("", "other players' cards stay face down before the reveal");

        GraphQLClient.ErrorCode(await host.RevealAsync(ticketId)).ShouldBeNull();

        var afterReveal = await player.VotesAsync(ticketId);
        afterReveal[host.UserId].ShouldBe("3");
        afterReveal[player.UserId].ShouldBe("8");
    }

    [Fact]
    public async Task VoteAgain_ClearsVotesAndHidesTheCards()
    {
        var (host, player, gameId, ticketId) = await GameWithTicketAsync();
        await player.VoteAsync(ticketId, "5");
        await host.RevealAsync(ticketId);

        var data = await host.RunAsync(
            "mutation($input: ReVoteTicketInput!) { reVoteTicket(input: $input) { ticket { votesRevealed } errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId, ticketId } });

        var payload = data.GetProperty("reVoteTicket");
        GraphQLClient.ErrorCode(payload).ShouldBeNull();
        payload.GetProperty("ticket").GetProperty("votesRevealed").GetBoolean().ShouldBeFalse();
        (await host.VotesAsync(ticketId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Timer_StartsAndStops_AndIsVisibleToPlayersOnly()
    {
        var (host, player, gameId, _) = await GameWithTicketAsync();
        var outsider = await factory.CreateUserAsync("Outsider");
        const string timerQuery = "query($gameId: UUID!) { gameTimer(gameId: $gameId) { running durationSeconds } }";

        var started = await host.RunAsync(
            "mutation($input: StartTimerInput!) { startTimer(input: $input) { gameTimer { running } errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId, durationSeconds = 90 } });
        GraphQLClient.ErrorCode(started.GetProperty("startTimer")).ShouldBeNull();

        var seenByPlayer = (await player.RunAsync(timerQuery, new { gameId })).GetProperty("gameTimer");
        seenByPlayer.GetProperty("running").GetBoolean().ShouldBeTrue();
        seenByPlayer.GetProperty("durationSeconds").GetInt32().ShouldBe(90);
        (await outsider.RunAsync(timerQuery, new { gameId })).GetProperty("gameTimer").ValueKind.ShouldBe(JsonValueKind.Null);

        var stopped = await host.RunAsync(
            "mutation($input: StopTimerInput!) { stopTimer(input: $input) { errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId } });
        GraphQLClient.ErrorCode(stopped.GetProperty("stopTimer")).ShouldBeNull();

        var afterStop = (await player.RunAsync(timerQuery, new { gameId })).GetProperty("gameTimer");
        if (afterStop.ValueKind != JsonValueKind.Null)
        {
            afterStop.GetProperty("running").GetBoolean().ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Chat_IsReadableAndWritableByPlayersOnly()
    {
        var (_, player, gameId, _) = await GameWithTicketAsync();
        var outsider = await factory.CreateUserAsync("Outsider");
        const string send = "mutation($input: SendChatMessageInput!) { sendChatMessage(input: $input) { chatMessage { content } errors { ... on ApplicationError { code } } } }";
        const string read = "query($gameId: UUID!) { chatMessages(gameId: $gameId) { content playerName } }";

        var sent = await player.RunAsync(send, new { input = new { gameId, content = "Ready when you are" } });
        GraphQLClient.ErrorCode(sent.GetProperty("sendChatMessage")).ShouldBeNull();

        var messages = (await player.RunAsync(read, new { gameId })).GetProperty("chatMessages");
        messages.GetArrayLength().ShouldBe(1);
        messages[0].GetProperty("playerName").GetString().ShouldBe("Marko");

        (await outsider.RunAsync(read, new { gameId })).GetProperty("chatMessages").GetArrayLength().ShouldBe(0);
        (await factory.CreateAnonymous().RunAsync(read, new { gameId })).GetProperty("chatMessages").GetArrayLength().ShouldBe(0);

        var outsiderSend = await outsider.RunAsync(send, new { input = new { gameId, content = "hi" } });
        GraphQLClient.ErrorCode(outsiderSend.GetProperty("sendChatMessage")).ShouldBe("NotMember");
    }

    [Theory]
    [InlineData("de-DE,de;q=0.9", "Nur der Host des Spiels kann das tun.")]
    [InlineData("sr-Latn-RS", "Ovo može da uradi samo host igre.")]
    [InlineData("fr", "Only the game host can perform this action.")]
    public async Task ErrorMessages_FollowAcceptLanguage(string language, string expected)
    {
        var (_, player, gameId, _) = await GameWithTicketAsync();
        player.AcceptLanguage(language);

        var data = await player.RunAsync(
            "mutation($input: CreateTicketInput!) { createTicket(input: $input) { errors { ... on ApplicationError { code message } } } }",
            new { input = new { gameId, name = "Not mine", description = "" } });

        var error = data.GetProperty("createTicket").GetProperty("errors")[0];
        error.GetProperty("code").GetString().ShouldBe("NotHost");
        error.GetProperty("message").GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task GameReport_IsAPdf()
    {
        var (host, player, gameId, ticketId) = await GameWithTicketAsync();
        await player.VoteAsync(ticketId, "5");
        await host.RevealAsync(ticketId);

        var data = await host.RunAsync(
            "mutation($input: GenerateGameReportInput!) { generateGameReport(input: $input) { gameReport { fileName data } errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId } });

        var payload = data.GetProperty("generateGameReport");
        GraphQLClient.ErrorCode(payload).ShouldBeNull();
        var report = payload.GetProperty("gameReport");
        report.GetProperty("fileName").GetString().ShouldEndWith(".pdf");
        var header = report.GetProperty("data").EnumerateArray().Take(4).Select(b => (char)b.GetInt32());
        string.Concat(header).ShouldBe("%PDF");
    }
}
