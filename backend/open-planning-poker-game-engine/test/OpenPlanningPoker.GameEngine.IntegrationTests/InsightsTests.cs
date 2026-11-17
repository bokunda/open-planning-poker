namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>Estimation insights (OPP-117) through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class InsightsTests(GameEngineFactory factory)
{
    private const string CreateTeam =
        "mutation($input: CreateTeamInput!) { createTeam(input: $input) { team { id } errors { ... on ApplicationError { code } } } }";
    private const string CreateSprint =
        "mutation($input: CreateGameInput!) { createGame(input: $input) { game { id } errors { ... on Error { message } } } }";
    private const string SetFinal =
        "mutation($input: SetFinalEstimateInput!) { setFinalEstimate(input: $input) { ticket { id } errors { ... on ApplicationError { code } } } }";
    private const string Webhook =
        "mutation($input: SetTeamSummaryWebhookInput!) { setTeamSummaryWebhook(input: $input) { boolean errors { ... on ApplicationError { code } } } }";

    [Fact]
    public async Task Sprint_HasSessionStats_AndTheSummaryNeedsADestination()
    {
        var host = await factory.CreateUserAsync("Ana");
        var teamId = (await host.RunAsync(CreateTeam, new { input = new { name = "Checkout" } }))
            .GetProperty("createTeam").GetProperty("team").GetProperty("id").GetGuid();
        var gameId = (await host.RunAsync(CreateSprint, new { input = new { name = "Sprint 7", description = "", teamId } }))
            .GetProperty("createGame").GetProperty("game").GetProperty("id").GetGuid();
        await host.JoinAsync(gameId);
        var ticketId = (await host.CreateTicketAsync(gameId, "Pay with card")).GetProperty("ticket").GetProperty("id").GetGuid();
        await host.VoteAsync(ticketId, "3");
        await host.RevealAsync(ticketId);
        await host.RunAsync(SetFinal, new { input = new { ticketId, value = "3" } });

        var team = (await host.RunAsync("query($id: UUID!) { team(id: $id) { ... on Team { sprints { stats { revealed firstRoundConsensus firstRoundConsensusShare averageDiscussionSeconds } } } } }",
            new { id = teamId })).GetProperty("team");
        var stats = team.GetProperty("sprints")[0].GetProperty("stats");
        stats.GetProperty("revealed").GetInt32().ShouldBe(1);
        stats.GetProperty("firstRoundConsensus").GetInt32().ShouldBe(1);
        stats.GetProperty("firstRoundConsensusShare").GetDouble().ShouldBe(1);
        stats.GetProperty("averageDiscussionSeconds").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Number);

        var summary = (await host.RunAsync("query($gameId: UUID!) { sessionSummary(gameId: $gameId) { ... on SessionSummaryInfo { text destination } } }",
            new { gameId })).GetProperty("sessionSummary");
        summary.GetProperty("text").GetString()!.ShouldContain("1 of 1 tickets estimated, 3 points");
        summary.GetProperty("destination").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Null);

        var post = await host.RunAsync("mutation($input: PostSessionSummaryInput!) { postSessionSummary(input: $input) { errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId } });
        GraphQLClient.ErrorCode(post.GetProperty("postSessionSummary")).ShouldBe("NoSummaryDestination");

        var bad = await host.RunAsync(Webhook, new { input = new { teamId, url = "https://example.com/hook" } });
        GraphQLClient.ErrorCode(bad.GetProperty("setTeamSummaryWebhook")).ShouldBe("InvalidWebhook");
        var ok = await host.RunAsync(Webhook, new { input = new { teamId, url = "https://hooks.slack.com/services/T000/B000/XXXX" } });
        GraphQLClient.ErrorCode(ok.GetProperty("setTeamSummaryWebhook")).ShouldBeNull();

        var accuracy = (await host.RunAsync("query($teamId: UUID!) { teamAccuracy(teamId: $teamId) { ... on TeamAccuracyInfo { measured pending cards { value } } } }",
            new { teamId })).GetProperty("teamAccuracy");
        accuracy.GetProperty("measured").GetInt32().ShouldBe(0);
    }
}
