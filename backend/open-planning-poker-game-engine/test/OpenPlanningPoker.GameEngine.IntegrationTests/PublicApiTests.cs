using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>The public API and webhooks of a team (OPP-141) through GraphQL and REST.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class PublicApiTests(GameEngineFactory factory)
{
    private const string Errors = "errors { ... on ApplicationError { code } }";
    private const string CreateTeam = "mutation($input: CreateTeamInput!) { createTeam(input: $input) { team { id } } }";
    private const string CreateSprint = "mutation($input: CreateGameInput!) { createGame(input: $input) { game { id } } }";
    private const string SetFinal = "mutation($input: SetFinalEstimateInput!) { setFinalEstimate(input: $input) { ticket { id } } }";
    private static readonly string CreateToken = $"mutation($input: CreateTeamApiTokenInput!) {{ createTeamApiToken(input: $input) {{ createdTeamApiToken {{ token info {{ id prefix active }} }} {Errors} }} }}";
    private static readonly string RevokeToken = $"mutation($input: RevokeTeamApiTokenInput!) {{ revokeTeamApiToken(input: $input) {{ boolean {Errors} }} }}";
    private static readonly string CreateWebhook = $"mutation($input: CreateTeamWebhookInput!) {{ createTeamWebhook(input: $input) {{ createdTeamWebhook {{ secret info {{ id events }} }} {Errors} }} }}";
    private const string Tokens = "query($id: UUID!) { team(id: $id) { ... on Team { apiTokens { name lastUsedOn active } webhooks { url } } } }";

    private async Task<Guid> TeamAsync(GraphQLClient owner, string name) =>
        (await owner.RunAsync(CreateTeam, new { input = new { name } })).GetProperty("createTeam").GetProperty("team").GetProperty("id").GetGuid();

    private async Task<Guid> SprintAsync(GraphQLClient owner, Guid teamId, string name)
    {
        var gameId = (await owner.RunAsync(CreateSprint, new { input = new { name, description = "", teamId } }))
            .GetProperty("createGame").GetProperty("game").GetProperty("id").GetGuid();
        await owner.JoinAsync(gameId);
        return gameId;
    }

    private HttpClient Api(string? token)
    {
        var http = factory.CreateClient();
        if (token is not null) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    [Fact]
    public async Task OwnerCreatesAToken_TheApiReadsOnlyThatTeam_AndRevokingStopsIt()
    {
        var owner = await factory.CreateUserAsync("Ana");
        var member = await factory.CreateUserAsync("Marko");
        var teamId = await TeamAsync(owner, "Payments");
        var otherTeam = await TeamAsync(owner, "Search");
        var gameId = await SprintAsync(owner, teamId, "Sprint 1");
        var otherGame = await SprintAsync(owner, otherTeam, "Elsewhere");

        var ticketId = (await owner.CreateTicketAsync(gameId, "OPP-1 Login")).GetProperty("ticket").GetProperty("id").GetGuid();
        await owner.VoteAsync(ticketId, "5");
        await owner.RevealAsync(ticketId);
        await owner.RunAsync(SetFinal, new { input = new { ticketId, value = "5" } });

        // Only the owner creates tokens.
        GraphQLClient.ErrorCode((await member.RunAsync(CreateToken, new { input = new { teamId, name = "CI" } })).GetProperty("createTeamApiToken"))
            .ShouldBe("NotTeamOwner");
        var created = (await owner.RunAsync(CreateToken, new { input = new { teamId, name = "Reporting" } }))
            .GetProperty("createTeamApiToken").GetProperty("createdTeamApiToken");
        var token = created.GetProperty("token").GetString()!;
        token.ShouldStartWith("opp_");
        created.GetProperty("info").GetProperty("prefix").GetString().ShouldBe(token[..12]);

        (await Api(null).GetAsync("/api/v1/sessions")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Api("opp_wrong").GetAsync("/api/v1/sessions")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var api = Api(token);
        var team = await api.GetFromJsonAsync<JsonElement>("/api/v1/team");
        team.GetProperty("name").GetString().ShouldBe("Payments");

        var sessions = await api.GetFromJsonAsync<JsonElement>("/api/v1/sessions?take=10");
        sessions.GetProperty("total").GetInt32().ShouldBe(1);
        sessions.GetProperty("items")[0].GetProperty("id").GetGuid().ShouldBe(gameId);
        sessions.GetProperty("items")[0].GetProperty("points").GetDouble().ShouldBe(5);

        var tickets = await api.GetFromJsonAsync<JsonElement>($"/api/v1/sessions/{gameId}/tickets");
        tickets.GetProperty("items")[0].GetProperty("estimate").GetString().ShouldBe("5");
        tickets.GetProperty("items")[0].TryGetProperty("votes", out _).ShouldBeFalse();

        // Another team's session is not visible with this token.
        (await api.GetAsync($"/api/v1/sessions/{otherGame}/tickets")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var listed = (await owner.RunAsync(Tokens, new { id = teamId })).GetProperty("team").GetProperty("apiTokens")[0];
        listed.GetProperty("lastUsedOn").ValueKind.ShouldNotBe(JsonValueKind.Null);

        var tokenId = created.GetProperty("info").GetProperty("id").GetGuid();
        GraphQLClient.ErrorCode((await owner.RunAsync(RevokeToken, new { input = new { teamId, tokenId } })).GetProperty("revokeTeamApiToken")).ShouldBeNull();
        (await Api(token).GetAsync("/api/v1/sessions")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhooks_NeedAPublicHttpsAddress_AndShowTheSecretOnce()
    {
        var owner = await factory.CreateUserAsync("Ana");
        var teamId = await TeamAsync(owner, "Payments");

        var internalUrl = (await owner.RunAsync(CreateWebhook, new { input = new { teamId, url = "https://169.254.169.254/x", events = new[] { "ticket.estimated" } } }))
            .GetProperty("createTeamWebhook");
        GraphQLClient.ErrorCode(internalUrl).ShouldBe("InvalidWebhookUrl");
        GraphQLClient.ErrorCode((await owner.RunAsync(CreateWebhook, new { input = new { teamId, url = "https://example.com/x", events = new[] { "ticket.deleted" } } }))
            .GetProperty("createTeamWebhook")).ShouldBe("InvalidWebhookEvents");

        var created = (await owner.RunAsync(CreateWebhook, new { input = new { teamId, url = "https://example.com/opp", events = new[] { "ticket.estimated", "session.finished" } } }))
            .GetProperty("createTeamWebhook").GetProperty("createdTeamWebhook");
        created.GetProperty("secret").GetString()!.ShouldStartWith("whsec_");
        created.GetProperty("info").GetProperty("events").GetArrayLength().ShouldBe(2);

        var listed = (await owner.RunAsync(Tokens, new { id = teamId })).GetProperty("team").GetProperty("webhooks");
        listed[0].GetProperty("url").GetString().ShouldBe("https://example.com/opp");
    }
}
