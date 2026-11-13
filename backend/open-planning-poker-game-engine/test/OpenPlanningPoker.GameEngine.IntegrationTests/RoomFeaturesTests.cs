namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>Room management from 2.2 (roles, co-hosts, host transfer, teams) through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class RoomFeaturesTests(GameEngineFactory factory)
{
    private const string SetRole =
        "mutation($input: SetPlayerRoleInput!) { setPlayerRole(input: $input) { boolean errors { ... on ApplicationError { code } } } }";

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
    public async Task Host_MakesAPlayerObserver_WhoThenCannotVote()
    {
        var (host, player, gameId, ticketId) = await GameWithTicketAsync();

        var data = await host.RunAsync(SetRole, new { input = new { gameId, playerId = player.UserId, role = "OBSERVER" } });
        GraphQLClient.ErrorCode(data.GetProperty("setPlayerRole")).ShouldBeNull();

        GraphQLClient.ErrorCode(await player.VoteAsync(ticketId, "3")).ShouldBe("Observer");
    }

    [Fact]
    public async Task Player_SwitchesOwnRole_BackAndForth()
    {
        var (_, player, gameId, ticketId) = await GameWithTicketAsync();

        var observer = await player.RunAsync(SetRole, new { input = new { gameId, role = "OBSERVER" } });
        GraphQLClient.ErrorCode(observer.GetProperty("setPlayerRole")).ShouldBeNull();

        var back = await player.RunAsync(SetRole, new { input = new { gameId, role = "PLAYER" } });
        GraphQLClient.ErrorCode(back.GetProperty("setPlayerRole")).ShouldBeNull();
        GraphQLClient.ErrorCode(await player.VoteAsync(ticketId, "5")).ShouldBeNull();
    }

    [Fact]
    public async Task Host_MakesCoHost_AndTransfersTheGame()
    {
        var (host, player, gameId, _) = await GameWithTicketAsync();

        var coHost = await host.RunAsync(
            "mutation($input: SetCoHostInput!) { setCoHost(input: $input) { boolean errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId, playerId = player.UserId, isCoHost = true } });
        GraphQLClient.ErrorCode(coHost.GetProperty("setCoHost")).ShouldBeNull();

        var transfer = await host.RunAsync(
            "mutation($input: TransferHostInput!) { transferHost(input: $input) { boolean errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId, playerId = player.UserId } });
        GraphQLClient.ErrorCode(transfer.GetProperty("transferHost")).ShouldBeNull();

        var game = await host.RunAsync("query($id: UUID!) { game(id: $id) { ... on Game { hostId } } }", new { id = gameId });
        game.GetProperty("game").GetProperty("hostId").GetGuid().ShouldBe(player.UserId);
    }
}
