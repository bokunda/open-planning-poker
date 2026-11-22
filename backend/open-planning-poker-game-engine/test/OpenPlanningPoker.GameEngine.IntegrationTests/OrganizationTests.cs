namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>Organizations with teams and roles (OPP-154, OPP-157) through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class OrganizationTests(GameEngineFactory factory)
{
    private const string Errors = "errors { ... on ApplicationError { code } }";
    private const string CreateTeam = "mutation($input: CreateTeamInput!) { createTeam(input: $input) { team { id } } }";
    private static readonly string Create = $"mutation($input: CreateOrganizationInput!) {{ createOrganization(input: $input) {{ organizationInfo {{ id myRole inviteToken }} {Errors} }} }}";
    private static readonly string Join = $"mutation($input: JoinOrganizationInput!) {{ joinOrganization(input: $input) {{ boolean {Errors} }} }}";
    private static readonly string AddTeam = $"mutation($input: AddTeamToOrganizationInput!) {{ addTeamToOrganization(input: $input) {{ boolean {Errors} }} }}";
    private static readonly string SetRole = $"mutation($input: SetOrganizationRoleInput!) {{ setOrganizationRole(input: $input) {{ boolean {Errors} }} }}";
    private const string Get = "query($id: UUID!) { organization(id: $id) { ... on OrganizationInfo { name myRole inviteToken members { id role } teams { id } comparison { teamId sprints } } ... on ApplicationError { code } } }";

    [Fact]
    public async Task Members_JoinWithTheLink_OwnersMoveTheirTeams_AndOnlyAdminsSeeTheNumbers()
    {
        var admin = await factory.CreateUserAsync("Ana");
        var owner = await factory.CreateUserAsync("Marko");
        var stranger = await factory.CreateUserAsync("Eve");

        var created = (await admin.RunAsync(Create, new { input = new { name = "Acme" } })).GetProperty("createOrganization").GetProperty("organizationInfo");
        var id = created.GetProperty("id").GetGuid();
        created.GetProperty("myRole").GetString().ShouldBe("ADMIN");
        var token = created.GetProperty("inviteToken").GetString()!;

        GraphQLClient.ErrorCode((await stranger.RunAsync(Join, new { input = new { id, token = "wrong" } })).GetProperty("joinOrganization")).ShouldBe("InvalidOrganizationInvite");
        GraphQLClient.ErrorCode((await owner.RunAsync(Join, new { input = new { id, token } })).GetProperty("joinOrganization")).ShouldBeNull();

        var teamId = (await owner.RunAsync(CreateTeam, new { input = new { name = "Payments" } })).GetProperty("createTeam").GetProperty("team").GetProperty("id").GetGuid();
        GraphQLClient.ErrorCode((await admin.RunAsync(AddTeam, new { input = new { id, teamId } })).GetProperty("addTeamToOrganization")).ShouldBe("NotTeamOwner");
        GraphQLClient.ErrorCode((await owner.RunAsync(AddTeam, new { input = new { id, teamId } })).GetProperty("addTeamToOrganization")).ShouldBeNull();

        var asMember = (await owner.RunAsync(Get, new { id })).GetProperty("organization");
        asMember.GetProperty("myRole").GetString().ShouldBe("MEMBER");
        asMember.GetProperty("inviteToken").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Null);
        asMember.GetProperty("teams").GetArrayLength().ShouldBe(1);
        asMember.GetProperty("comparison").GetArrayLength().ShouldBe(0);

        var asAdmin = (await admin.RunAsync(Get, new { id })).GetProperty("organization");
        asAdmin.GetProperty("comparison")[0].GetProperty("teamId").GetGuid().ShouldBe(teamId);

        var asStranger = (await stranger.RunAsync(Get, new { id })).GetProperty("organization");
        asStranger.GetProperty("members").GetArrayLength().ShouldBe(0);

        // The last admin cannot step down.
        GraphQLClient.ErrorCode((await admin.RunAsync(SetRole, new { input = new { id, playerId = admin.UserId, role = "MEMBER" } })).GetProperty("setOrganizationRole"))
            .ShouldBe("LastOrganizationAdmin");
    }
}
