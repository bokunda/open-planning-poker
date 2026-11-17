namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>Reference stories from past sprints (OPP-115) through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class ReferenceTests(GameEngineFactory factory)
{
    private const string CreateTeam =
        "mutation($input: CreateTeamInput!) { createTeam(input: $input) { team { id } errors { ... on ApplicationError { code } } } }";
    private const string CreateSprint =
        "mutation($input: CreateGameInput!) { createGame(input: $input) { game { id } errors { ... on Error { message } } } }";
    private const string SetFinal =
        "mutation($input: SetFinalEstimateInput!) { setFinalEstimate(input: $input) { ticket { id } errors { ... on ApplicationError { code } } } }";
    private const string Pin =
        "mutation($input: PinTeamAnchorInput!) { pinTeamAnchor(input: $input) { teamAnchor { id value note ticket { name } } errors { ... on ApplicationError { code } } } }";
    private const string Reference =
        "query($ticketId: UUID!) { ticketReference(ticketId: $ticketId) { ... on TicketReference { teamId anchors { value ticket { name } } similar { name estimate } } } }";
    private const string History =
        "query($id: UUID!, $search: String) { team(id: $id) { ... on Team { history(search: $search) { totalCount items { name estimate sprintName } } } } }";

    [Fact]
    public async Task EstimatedTicket_CanBePinned_AndShowsUpWhileVoting()
    {
        var host = await factory.CreateUserAsync("Ana");
        var teamId = (await host.RunAsync(CreateTeam, new { input = new { name = "Payments" } }))
            .GetProperty("createTeam").GetProperty("team").GetProperty("id").GetGuid();

        var sprint1 = (await host.RunAsync(CreateSprint, new { input = new { name = "Sprint 1", description = "", teamId } }))
            .GetProperty("createGame").GetProperty("game").GetProperty("id").GetGuid();
        await host.JoinAsync(sprint1);
        var done = (await host.CreateTicketAsync(sprint1, "Refund a card payment")).GetProperty("ticket").GetProperty("id").GetGuid();
        var notRevealed = (await host.CreateTicketAsync(sprint1, "Export invoices")).GetProperty("ticket").GetProperty("id").GetGuid();
        await host.VoteAsync(done, "5");
        await host.RevealAsync(done);
        await host.RunAsync(SetFinal, new { input = new { ticketId = done, value = "5" } });

        var unestimated = await host.RunAsync(Pin, new { input = new { teamId, ticketId = notRevealed } });
        GraphQLClient.ErrorCode(unestimated.GetProperty("pinTeamAnchor")).ShouldBe("TicketNotEstimated");

        var pinned = await host.RunAsync(Pin, new { input = new { teamId, ticketId = done, note = "One API call, small UI" } });
        var anchor = pinned.GetProperty("pinTeamAnchor").GetProperty("teamAnchor");
        anchor.GetProperty("value").GetString().ShouldBe("5");
        anchor.GetProperty("note").GetString().ShouldBe("One API call, small UI");

        var sprint2 = (await host.RunAsync(CreateSprint, new { input = new { name = "Sprint 2", description = "", teamId } }))
            .GetProperty("createGame").GetProperty("game").GetProperty("id").GetGuid();
        await host.JoinAsync(sprint2);
        var current = (await host.CreateTicketAsync(sprint2, "Refund a bank transfer payment")).GetProperty("ticket").GetProperty("id").GetGuid();

        var reference = (await host.RunAsync(Reference, new { ticketId = current })).GetProperty("ticketReference");
        reference.GetProperty("teamId").GetGuid().ShouldBe(teamId);
        reference.GetProperty("anchors")[0].GetProperty("ticket").GetProperty("name").GetString().ShouldBe("Refund a card payment");
        reference.GetProperty("similar")[0].GetProperty("estimate").GetString().ShouldBe("5");

        var history = (await host.RunAsync(History, new { id = teamId, search = "refund" })).GetProperty("team").GetProperty("history");
        history.GetProperty("totalCount").GetInt32().ShouldBe(1);
        history.GetProperty("items")[0].GetProperty("sprintName").GetString().ShouldBe("Sprint 1");
    }
}

/// <summary>GitHub, Linear and Azure DevOps (OPP-116) stay off until their OAuth apps are configured.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class TrackerTests(GameEngineFactory factory)
{
    [Fact]
    public async Task Trackers_AreListed_AndOffWithoutConfiguration()
    {
        var host = await factory.CreateUserAsync("Ana");
        var trackers = (await host.RunAsync("query { trackers { provider enabled connected } }")).GetProperty("trackers");
        trackers.EnumerateArray().Select(t => t.GetProperty("provider").GetString()).ShouldBe(["github", "linear", "azure-devops"], ignoreOrder: true);
        trackers.EnumerateArray().ShouldAllBe(t => !t.GetProperty("enabled").GetBoolean());

        var url = await host.RunAsync("query { trackerAuthorizeUrl(provider: \"github\", returnPath: \"/game\") { ... on ApplicationError { code } } }");
        url.GetProperty("trackerAuthorizeUrl").GetProperty("code").GetString().ShouldBe("TrackerNotConfigured");
    }
}
