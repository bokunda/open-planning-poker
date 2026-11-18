using System.Text.Json;

namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>The sorting board for long backlogs (OPP-137) through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class SortingTests(GameEngineFactory factory)
{
    private const string Errors = "errors { ... on ApplicationError { code } }";
    private const string Board = "open columns tickets { ticketId column moves contested }";

    private static readonly string Open =
        $"mutation($input: SetSortingOpenInput!) {{ setSortingOpen(input: $input) {{ sortingBoardInfo {{ {Board} }} {Errors} }} }}";
    private static readonly string Move =
        $"mutation($input: MoveSortedTicketInput!) {{ moveSortedTicket(input: $input) {{ sortedTicket {{ column moves contested }} {Errors} }} }}";
    private static readonly string Clear =
        $"mutation($input: ClearSortContestedInput!) {{ clearSortContested(input: $input) {{ sortedTicket {{ contested moves }} {Errors} }} }}";
    private static readonly string Apply =
        $"mutation($input: ApplySortedEstimatesInput!) {{ applySortedEstimates(input: $input) {{ sortedEstimatesApplied {{ applied contested unsorted }} {Errors} }} }}";
    private static readonly string Get =
        $"query($gameId: UUID!) {{ sortingBoard(gameId: $gameId) {{ ... on SortingBoardInfo {{ {Board} }} ... on ApplicationError {{ code }} }} }}";

    private static Guid Id(JsonElement created) => created.GetProperty("ticket").GetProperty("id").GetGuid();

    [Fact]
    public async Task Board_IsOpenedByTheHost_AndShowsTheDeckWithoutSpecialCards()
    {
        var host = await factory.CreateUserAsync("Ana");
        var player = await factory.CreateUserAsync("Marko");
        var gameId = await host.CreateGameAsync();
        await host.JoinAsync(gameId);
        await player.JoinAsync(gameId);

        var byPlayer = await player.RunAsync(Open, new { input = new { gameId, open = true } });
        GraphQLClient.ErrorCode(byPlayer.GetProperty("setSortingOpen")).ShouldBe("NotHost");

        var opened = await host.RunAsync(Open, new { input = new { gameId, open = true } });
        var board = opened.GetProperty("setSortingOpen").GetProperty("sortingBoardInfo");
        board.GetProperty("open").GetBoolean().ShouldBeTrue();
        var columns = board.GetProperty("columns").EnumerateArray().Select(c => c.GetString()).ToList();
        columns.ShouldContain("13");
        columns.ShouldNotContain("❓");
        columns.ShouldNotContain("⏸️");

        var seen = await player.RunAsync(Get, new { gameId });
        seen.GetProperty("sortingBoard").GetProperty("open").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Moves_AreChecked_FlagDisagreement_AndApplySkipsContested()
    {
        var host = await factory.CreateUserAsync("Ana");
        var player = await factory.CreateUserAsync("Marko");
        var gameId = await host.CreateGameAsync();
        await host.JoinAsync(gameId);
        await player.JoinAsync(gameId);
        var search = Id(await host.CreateTicketAsync(gameId, "Search"));
        var export = Id(await host.CreateTicketAsync(gameId, "Export"));
        var later = Id(await host.CreateTicketAsync(gameId, "Later"));

        var closed = await player.RunAsync(Move, new { input = new { ticketId = search, column = "3" } });
        GraphQLClient.ErrorCode(closed.GetProperty("moveSortedTicket")).ShouldBe("SortingClosed");

        await host.RunAsync(Open, new { input = new { gameId, open = true } });
        var unknown = await player.RunAsync(Move, new { input = new { ticketId = search, column = "❓" } });
        GraphQLClient.ErrorCode(unknown.GetProperty("moveSortedTicket")).ShouldBe("UnknownColumn");

        await player.RunAsync(Move, new { input = new { ticketId = search, column = "3" } });
        await host.RunAsync(Move, new { input = new { ticketId = export, column = "5" } });
        var disagreed = await player.RunAsync(Move, new { input = new { ticketId = export, column = "13" } });
        disagreed.GetProperty("moveSortedTicket").GetProperty("sortedTicket").GetProperty("contested").GetBoolean().ShouldBeTrue();

        var applied = await host.RunAsync(Apply, new { input = new { gameId } });
        var result = applied.GetProperty("applySortedEstimates").GetProperty("sortedEstimatesApplied");
        result.GetProperty("applied").GetInt32().ShouldBe(1);
        result.GetProperty("contested").GetInt32().ShouldBe(1);
        result.GetProperty("unsorted").GetInt32().ShouldBe(1);

        var tickets = await host.RunAsync("query($gameId: UUID!) { tickets(gameId: $gameId) { ... on ApiCollectionOfTicket { items { id finalEstimate } } } }", new { gameId });
        var byId = tickets.GetProperty("tickets").GetProperty("items").EnumerateArray()
            .ToDictionary(t => t.GetProperty("id").GetGuid(),
                t => t.GetProperty("finalEstimate").ValueKind == JsonValueKind.Null ? null : t.GetProperty("finalEstimate").GetString());
        byId[search].ShouldBe("3");
        byId[export].ShouldBeNull();
        byId[later].ShouldBeNull();

        // An estimated ticket stays in its column.
        var locked = await player.RunAsync(Move, new { input = new { ticketId = search, column = "8" } });
        GraphQLClient.ErrorCode(locked.GetProperty("moveSortedTicket")).ShouldBe("AlreadyEstimated");

        var byPlayer = await player.RunAsync(Clear, new { input = new { ticketId = export } });
        GraphQLClient.ErrorCode(byPlayer.GetProperty("clearSortContested")).ShouldBe("NotHost");
        var cleared = await host.RunAsync(Clear, new { input = new { ticketId = export } });
        cleared.GetProperty("clearSortContested").GetProperty("sortedTicket").GetProperty("contested").GetBoolean().ShouldBeFalse();

        var second = await host.RunAsync(Apply, new { input = new { gameId } });
        second.GetProperty("applySortedEstimates").GetProperty("sortedEstimatesApplied").GetProperty("applied").GetInt32().ShouldBe(1);
    }
}
