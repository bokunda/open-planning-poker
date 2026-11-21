using System.Text.Json;

namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>Sprint retrospectives (OPP-162) through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class RetroTests(GameEngineFactory factory)
{
    private const string Errors = "errors { ... on ApplicationError { code } }";
    private const string Info =
        "id phase columns dotsLeft canFacilitate countPerColumn cards { id column text mine authorId groupId votes myVotes } actionItems { id text ownerId done } carriedOver { id text done }";

    private const string CreateTeam =
        "mutation($input: CreateTeamInput!) { createTeam(input: $input) { team { id } errors { ... on ApplicationError { code } } } }";
    private const string JoinTeam =
        "mutation($input: JoinTeamInput!) { joinTeam(input: $input) { boolean errors { ... on ApplicationError { code } } } }";
    private const string CreateSprint =
        "mutation($input: CreateGameInput!) { createGame(input: $input) { game { id } errors { ... on Error { message } } } }";

    private static readonly string Start = $"mutation($input: StartRetroInput!) {{ startRetro(input: $input) {{ retroInfo {{ {Info} }} {Errors} }} }}";
    private static readonly string Get = $"query($gameId: UUID!) {{ retro(gameId: $gameId) {{ ... on RetroInfo {{ {Info} }} ... on ApplicationError {{ code }} }} }}";
    private static readonly string Phase = $"mutation($input: SetRetroPhaseInput!) {{ setRetroPhase(input: $input) {{ retroInfo {{ phase }} {Errors} }} }}";
    private static readonly string AddCard = $"mutation($input: AddRetroCardInput!) {{ addRetroCard(input: $input) {{ retroCardInfo {{ id }} {Errors} }} }}";
    private static readonly string Group = $"mutation($input: GroupRetroCardInput!) {{ groupRetroCard(input: $input) {{ boolean {Errors} }} }}";
    private static readonly string Vote = $"mutation($input: VoteRetroCardInput!) {{ voteRetroCard(input: $input) {{ int {Errors} }} }}";
    private static readonly string AddItem = $"mutation($input: AddRetroActionItemInput!) {{ addRetroActionItem(input: $input) {{ retroActionItemInfo {{ id }} {Errors} }} }}";
    private const string OpenItems = "query($id: UUID!) { team(id: $id) { ... on Team { openActionItems { text } retros { gameId phase } } } }";
    private const string Export = "query($retroId: UUID!) { retroExport(retroId: $retroId) { ... on RetroExportFile { fileName data } ... on ApplicationError { code } } }";

    private async Task<(GraphQLClient Owner, GraphQLClient Member, Guid TeamId)> TeamAsync()
    {
        var owner = await factory.CreateUserAsync("Ana");
        var member = await factory.CreateUserAsync("Marko");
        var teamId = (await owner.RunAsync(CreateTeam, new { input = new { name = "Payments" } }))
            .GetProperty("createTeam").GetProperty("team").GetProperty("id").GetGuid();
        await member.RunAsync(JoinTeam, new { input = new { teamId } });
        return (owner, member, teamId);
    }

    private async Task<Guid> SprintAsync(GraphQLClient owner, GraphQLClient member, Guid teamId, string name)
    {
        var gameId = (await owner.RunAsync(CreateSprint, new { input = new { name, description = "", teamId } }))
            .GetProperty("createGame").GetProperty("game").GetProperty("id").GetGuid();
        await owner.JoinAsync(gameId);
        await member.JoinAsync(gameId);
        return gameId;
    }

    private static JsonElement Retro(JsonElement data) => data.GetProperty("retro");

    private async Task<Guid> CardAsync(GraphQLClient client, Guid retroId, int column, string text) =>
        (await client.RunAsync(AddCard, new { input = new { retroId, column, text } }))
            .GetProperty("addRetroCard").GetProperty("retroCardInfo").GetProperty("id").GetGuid();

    private static async Task MoveAsync(GraphQLClient client, Guid retroId, string phase) =>
        GraphQLClient.ErrorCode((await client.RunAsync(Phase, new { input = new { retroId, phase } })).GetProperty("setRetroPhase")).ShouldBeNull();

    [Fact]
    public async Task Retro_HidesCardsWhileWriting_GroupsAndVotes_ThenCarriesActionItemsOver()
    {
        var (owner, member, teamId) = await TeamAsync();
        var gameId = await SprintAsync(owner, member, teamId, "Sprint 1");

        // Only the owner or the host starts it, once per sprint.
        var byMember = (await member.RunAsync(Start, new { input = new { teamId, gameId, template = "went-well" } })).GetProperty("startRetro");
        GraphQLClient.ErrorCode(byMember).ShouldBe("NotTeamOwner");
        var started = (await owner.RunAsync(Start, new { input = new { teamId, gameId, template = "went-well" } }))
            .GetProperty("startRetro").GetProperty("retroInfo");
        var retroId = started.GetProperty("id").GetGuid();
        started.GetProperty("columns").GetInt32().ShouldBe(3);
        started.GetProperty("canFacilitate").GetBoolean().ShouldBeTrue();
        GraphQLClient.ErrorCode((await owner.RunAsync(Start, new { input = new { teamId, gameId, template = "4ls" } })).GetProperty("startRetro"))
            .ShouldBe("RetroAlreadyStarted");

        var pairing = await CardAsync(owner, retroId, 0, "Pairing worked");
        var reviews = await CardAsync(member, retroId, 1, "Reviews were slow");
        var smallPrs = await CardAsync(member, retroId, 1, "PRs too big");

        // Writing: the owner sees only their own card, and counts for the rest.
        var ownerView = Retro(await owner.RunAsync(Get, new { gameId }));
        ownerView.GetProperty("cards").GetArrayLength().ShouldBe(1);
        ownerView.GetProperty("countPerColumn").EnumerateArray().Select(c => c.GetInt32()).ShouldBe([1, 2, 0]);
        Retro(await member.RunAsync(Get, new { gameId })).GetProperty("canFacilitate").GetBoolean().ShouldBeFalse();

        // Players cannot move the retro on.
        GraphQLClient.ErrorCode((await member.RunAsync(Phase, new { input = new { retroId, phase = "GROUPING" } })).GetProperty("setRetroPhase"))
            .ShouldBe("NotHost");
        await MoveAsync(owner, retroId, "GROUPING");
        Retro(await owner.RunAsync(Get, new { gameId })).GetProperty("cards").GetArrayLength().ShouldBe(3);

        GraphQLClient.ErrorCode((await member.RunAsync(Group, new { input = new { cardId = smallPrs, targetCardId = reviews } })).GetProperty("groupRetroCard"))
            .ShouldBeNull();

        await MoveAsync(owner, retroId, "VOTING");
        (await member.RunAsync(Vote, new { input = new { cardId = smallPrs, add = true } })).GetProperty("voteRetroCard").GetProperty("int").GetInt32().ShouldBe(2);
        await member.RunAsync(Vote, new { input = new { cardId = reviews, add = true } });
        await owner.RunAsync(Vote, new { input = new { cardId = pairing, add = true } });
        await member.RunAsync(Vote, new { input = new { cardId = reviews, add = true } });
        GraphQLClient.ErrorCode((await member.RunAsync(Vote, new { input = new { cardId = reviews, add = true } })).GetProperty("voteRetroCard"))
            .ShouldBe("NoDotsLeft");

        // Dots of others stay hidden while voting.
        Retro(await owner.RunAsync(Get, new { gameId })).GetProperty("cards").EnumerateArray()
            .ShouldAllBe(c => c.GetProperty("votes").ValueKind == JsonValueKind.Null);

        await MoveAsync(owner, retroId, "DISCUSSING");
        var results = Retro(await owner.RunAsync(Get, new { gameId })).GetProperty("cards");
        // The group (three dots on its head) comes first.
        results[0].GetProperty("id").GetGuid().ShouldBe(reviews);
        results[0].GetProperty("votes").GetInt32().ShouldBe(3);

        var item = (await owner.RunAsync(AddItem, new { input = new { retroId, text = "Review within a day", cardId = reviews, ownerId = member.UserId } }))
            .GetProperty("addRetroActionItem");
        GraphQLClient.ErrorCode(item).ShouldBeNull();
        var stranger = await factory.CreateUserAsync("Eve");
        GraphQLClient.ErrorCode((await owner.RunAsync(AddItem, new { input = new { retroId, text = "x", ownerId = stranger.UserId } })).GetProperty("addRetroActionItem"))
            .ShouldBe("InvalidActionItemOwner");

        var team = (await member.RunAsync(OpenItems, new { id = teamId })).GetProperty("team");
        team.GetProperty("openActionItems")[0].GetProperty("text").GetString().ShouldBe("Review within a day");
        team.GetProperty("retros")[0].GetProperty("phase").GetString().ShouldBe("DISCUSSING");

        var export = (await member.RunAsync(Export, new { retroId })).GetProperty("retroExport");
        var markdown = System.Text.Encoding.UTF8.GetString([.. export.GetProperty("data").EnumerateArray().Select(b => b.GetByte())]);
        markdown.ShouldContain("Review within a day — Marko");

        // The next sprint's retro opens with the open item.
        var next = await SprintAsync(owner, member, teamId, "Sprint 2");
        var nextRetro = (await owner.RunAsync(Start, new { input = new { teamId, gameId = next, template = "mad-sad-glad" } }))
            .GetProperty("startRetro").GetProperty("retroInfo");
        nextRetro.GetProperty("carriedOver")[0].GetProperty("text").GetString().ShouldBe("Review within a day");
    }
}
