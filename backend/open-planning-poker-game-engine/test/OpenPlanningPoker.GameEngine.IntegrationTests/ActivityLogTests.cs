using System.Text.Json;

namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>The activity log (GitHub #20) written by real mutations and read through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class ActivityLogTests(GameEngineFactory factory)
{
    private const string Log = """
        query($scope: AuditScope!, $scopeId: UUID!, $before: DateTime, $take: Int!) {
          activityLog(scope: $scope, scopeId: $scopeId, before: $before, take: $take) {
            ... on ActivityLog { hasMore entries { action type actorId actorName targetId targetName ticketId detail value createdOn } }
            ... on ApplicationError { code }
          }
        }
        """;

    private static async Task<JsonElement> LogAsync(GraphQLClient client, string scope, Guid scopeId, int take = 30, DateTimeOffset? before = null) =>
        (await client.RunAsync(Log, new { scope, scopeId, take, before })).GetProperty("activityLog");

    private static string? Code(JsonElement result) => result.TryGetProperty("code", out var code) ? code.GetString() : null;

    private static string[] Actions(JsonElement log) =>
        [.. log.GetProperty("entries").EnumerateArray().Select(entry => entry.GetProperty("action").GetString()!)];

    [Fact]
    public async Task HostActions_AreLogged_NewestFirst_AndOnlyHostsCanReadThem()
    {
        var host = await factory.CreateUserAsync("Ana");
        var player = await factory.CreateUserAsync("Marko");
        var gameId = await host.CreateGameAsync();
        await host.JoinAsync(gameId);
        await player.JoinAsync(gameId);

        var ticketId = (await host.CreateTicketAsync(gameId, "Checkout page")).GetProperty("ticket").GetProperty("id").GetGuid();
        GraphQLClient.ErrorCode(await player.VoteAsync(ticketId, "5")).ShouldBeNull();
        GraphQLClient.ErrorCode(await host.RevealAsync(ticketId)).ShouldBeNull();

        var deleted = await host.RunAsync(
            "mutation($input: DeleteTicketInput!) { deleteTicket(input: $input) { boolean errors { ... on ApplicationError { code } } } }",
            new { input = new { id = ticketId } });
        GraphQLClient.ErrorCode(deleted.GetProperty("deleteTicket")).ShouldBeNull();

        var removed = await host.RunAsync(
            "mutation($input: RemovePlayerInput!) { removePlayer(input: $input) { boolean errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId, playerId = player.UserId } });
        GraphQLClient.ErrorCode(removed.GetProperty("removePlayer")).ShouldBeNull();

        // A failed host action is not logged.
        GraphQLClient.ErrorCode(await player.RevealAsync(ticketId)).ShouldNotBeNull();

        var log = await LogAsync(host, "GAME", gameId);
        Actions(log).ShouldBe(["player.removed", "ticket.deleted", "votes.revealed", "ticket.created"]);

        var entries = log.GetProperty("entries").EnumerateArray().ToList();
        entries[0].GetProperty("targetId").GetGuid().ShouldBe(player.UserId);
        entries[0].GetProperty("targetName").GetString().ShouldBe("Marko");
        entries[0].GetProperty("type").GetString().ShouldBe("DELETE");
        // The deleted ticket keeps its name in the log.
        entries[1].GetProperty("detail").GetString().ShouldBe("Checkout page");
        entries[1].GetProperty("ticketId").GetGuid().ShouldBe(ticketId);
        entries.ShouldAllBe(entry => entry.GetProperty("actorId").GetGuid() == host.UserId);
        entries[2].GetProperty("actorName").GetString().ShouldBe("Ana");
        // Votes never show up in the log.
        log.GetRawText().ShouldNotContain("\"5\"");

        // Paging: two at a time, then the rest.
        var first = await LogAsync(host, "GAME", gameId, take: 2);
        first.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        var oldestShown = first.GetProperty("entries")[1].GetProperty("createdOn").GetDateTimeOffset();
        Actions(await LogAsync(host, "GAME", gameId, take: 2, before: oldestShown)).ShouldBe(["votes.revealed", "ticket.created"]);

        // Players (here: removed, but a member would get the same) and strangers cannot read it.
        var stranger = await factory.CreateUserAsync("Eve");
        Code(await LogAsync(stranger, "GAME", gameId)).ShouldBe("NotHost");
        Code(await LogAsync(factory.CreateAnonymous(), "GAME", gameId)).ShouldBe("Unauthenticated");
    }

    [Fact]
    public async Task SavingSettingsThatDidNotChange_IsNotLogged()
    {
        var host = await factory.CreateUserAsync("Ana");
        var gameId = await host.CreateGameAsync();
        await host.JoinAsync(gameId);
        var game = await host.RunAsync("query($id: UUID!) { game(id: $id) { ... on Game { settingsDetails { ... on Settings { id } } } } }", new { id = gameId });
        var settingsId = game.GetProperty("game").GetProperty("settingsDetails").GetProperty("id").GetGuid();

        const string Update = "mutation($input: UpdateSettingsInput!) { updateSettings(input: $input) { settings { deckSetup } errors { ... on ApplicationError { code } } } }";
        // The room saves the chosen deck right after creating a game: the default deck again is no change.
        GraphQLClient.ErrorCode((await host.RunAsync(Update, new { input = new { id = settingsId, gameId, deckSetup = "0,0.5,1,2,3,5,8,13,❓,⏸️" } })).GetProperty("updateSettings")).ShouldBeNull();
        Actions(await LogAsync(host, "GAME", gameId)).ShouldBeEmpty();

        GraphQLClient.ErrorCode((await host.RunAsync(Update, new { input = new { id = settingsId, gameId, deckSetup = "1,2,3" } })).GetProperty("updateSettings")).ShouldBeNull();
        var log = await LogAsync(host, "GAME", gameId);
        Actions(log).ShouldBe(["settings.deck"]);
        log.GetProperty("entries")[0].GetProperty("value").GetString().ShouldBe("1,2,3");
    }

    [Fact]
    public async Task TeamMembership_IsLogged_ForTheOwnerOnly()
    {
        var owner = await factory.CreateUserAsync("Ana");
        var member = await factory.CreateUserAsync("Marko");
        var teamId = (await owner.RunAsync("mutation($input: CreateTeamInput!) { createTeam(input: $input) { team { id } } }",
            new { input = new { name = "Payments" } })).GetProperty("createTeam").GetProperty("team").GetProperty("id").GetGuid();

        const string Join = "mutation($input: JoinTeamInput!) { joinTeam(input: $input) { boolean errors { ... on ApplicationError { code } } } }";
        const string Leave = "mutation($input: LeaveTeamInput!) { leaveTeam(input: $input) { boolean errors { ... on ApplicationError { code } } } }";
        GraphQLClient.ErrorCode((await member.RunAsync(Join, new { input = new { teamId } })).GetProperty("joinTeam")).ShouldBeNull();
        GraphQLClient.ErrorCode((await member.RunAsync(Leave, new { input = new { teamId } })).GetProperty("leaveTeam")).ShouldBeNull();

        var log = await LogAsync(owner, "TEAM", teamId);
        Actions(log).ShouldBe(["team.memberRemoved", "team.memberJoined"]);
        var left = log.GetProperty("entries")[0];
        // The member left on their own: the actor is the target.
        left.GetProperty("actorId").GetGuid().ShouldBe(member.UserId);
        left.GetProperty("targetId").GetGuid().ShouldBe(member.UserId);

        Code(await LogAsync(member, "TEAM", teamId)).ShouldBe("NotTeamOwner");
    }

    [Fact]
    public async Task UserLog_IsPrivate()
    {
        var ana = await factory.CreateUserAsync("Ana");
        var eve = await factory.CreateUserAsync("Eve");

        Actions(await LogAsync(ana, "USER", ana.UserId)).ShouldBeEmpty();
        Code(await LogAsync(eve, "USER", ana.UserId)).ShouldBe("Forbidden");
    }
}
