namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>Team capacity and the sprint plan (OPP-138) through the GraphQL API.</summary>
[Collection(GameEngineCollection.Name)]
public sealed class PlanningTests(GameEngineFactory factory)
{
    private const string Errors = "errors { ... on ApplicationError { code } }";
    private const string Plan = "sprintDays sprintDaysSet personDays plannedPoints members { playerId days isDefault } budget { enoughHistory basedOnCapacity sprintsUsed suggested low high } tickets { ticketId estimate points planned }";

    private const string CreateTeam =
        "mutation($input: CreateTeamInput!) { createTeam(input: $input) { team { id } errors { ... on ApplicationError { code } } } }";
    private const string JoinTeam =
        "mutation($input: JoinTeamInput!) { joinTeam(input: $input) { boolean errors { ... on ApplicationError { code } } } }";
    private const string CreateSprint =
        "mutation($input: CreateGameInput!) { createGame(input: $input) { game { id } errors { ... on Error { message } } } }";
    private const string SetFinal =
        "mutation($input: SetFinalEstimateInput!) { setFinalEstimate(input: $input) { ticket { id } errors { ... on ApplicationError { code } } } }";

    private static readonly string Get =
        $"query($teamId: UUID!, $gameId: UUID!) {{ sprintPlan(teamId: $teamId, gameId: $gameId) {{ ... on SprintPlan {{ {Plan} }} ... on ApplicationError {{ code }} }} }}";
    private static readonly string Days =
        $"mutation($input: SetSprintDaysInput!) {{ setSprintDays(input: $input) {{ sprintPlan {{ {Plan} }} {Errors} }} }}";
    private static readonly string Member =
        $"mutation($input: SetMemberCapacityInput!) {{ setMemberCapacity(input: $input) {{ sprintPlan {{ {Plan} }} {Errors} }} }}";
    private static readonly string Planned =
        $"mutation($input: SetTicketPlannedInput!) {{ setTicketPlanned(input: $input) {{ sprintPlan {{ {Plan} }} {Errors} }} }}";

    private async Task<Guid> SprintAsync(GraphQLClient host, Guid teamId, string name, params string[] estimates)
    {
        var gameId = (await host.RunAsync(CreateSprint, new { input = new { name, description = "", teamId } }))
            .GetProperty("createGame").GetProperty("game").GetProperty("id").GetGuid();
        await host.JoinAsync(gameId);
        foreach (var estimate in estimates)
        {
            var ticketId = (await host.CreateTicketAsync(gameId, $"Ticket {estimate}")).GetProperty("ticket").GetProperty("id").GetGuid();
            await host.VoteAsync(ticketId, estimate);
            await host.RevealAsync(ticketId);
            await host.RunAsync(SetFinal, new { input = new { ticketId, value = estimate } });
        }
        return gameId;
    }

    [Fact]
    public async Task Capacity_Budget_AndPlan()
    {
        var owner = await factory.CreateUserAsync("Ana");
        var member = await factory.CreateUserAsync("Marko");
        var teamId = (await owner.RunAsync(CreateTeam, new { input = new { name = "Payments" } }))
            .GetProperty("createTeam").GetProperty("team").GetProperty("id").GetGuid();
        await member.RunAsync(JoinTeam, new { input = new { teamId } });

        // Two past sprints of 8 and 13 points, then the one being planned.
        await SprintAsync(owner, teamId, "Sprint 1", "3", "5");
        await SprintAsync(owner, teamId, "Sprint 2", "8", "5");
        var gameId = await SprintAsync(owner, teamId, "Sprint 3", "5", "3", "8");

        var plan = (await member.RunAsync(Get, new { teamId, gameId })).GetProperty("sprintPlan");
        plan.GetProperty("sprintDays").GetInt32().ShouldBe(10);
        plan.GetProperty("personDays").GetDouble().ShouldBe(20);
        var budget = plan.GetProperty("budget");
        budget.GetProperty("enoughHistory").GetBoolean().ShouldBeTrue();
        budget.GetProperty("basedOnCapacity").GetBoolean().ShouldBeFalse();
        budget.GetProperty("suggested").GetInt32().ShouldBe(10); // (8 + 13) / 2
        budget.GetProperty("low").GetInt32().ShouldBe(8);
        budget.GetProperty("high").GetInt32().ShouldBe(13);

        // Only the owner sets the sprint length; a member sets their own days but nobody else's.
        var byMember = await member.RunAsync(Days, new { input = new { teamId, gameId, days = 8 } });
        GraphQLClient.ErrorCode(byMember.GetProperty("setSprintDays")).ShouldBe("NotTeamOwner");
        var tooLong = await owner.RunAsync(Days, new { input = new { teamId, gameId, days = 31 } });
        GraphQLClient.ErrorCode(tooLong.GetProperty("setSprintDays")).ShouldBe("InvalidSprintDays");
        await owner.RunAsync(Days, new { input = new { teamId, gameId, days = 8 } });

        var other = await member.RunAsync(Member, new { input = new { teamId, gameId, playerId = owner.UserId, days = 2 } });
        GraphQLClient.ErrorCode(other.GetProperty("setMemberCapacity")).ShouldBe("NotTeamOwner");
        var odd = await member.RunAsync(Member, new { input = new { teamId, gameId, playerId = member.UserId, days = 2.3 } });
        GraphQLClient.ErrorCode(odd.GetProperty("setMemberCapacity")).ShouldBe("InvalidMemberDays");
        var mine = (await member.RunAsync(Member, new { input = new { teamId, gameId, playerId = member.UserId, days = 4.5 } }))
            .GetProperty("setMemberCapacity").GetProperty("sprintPlan");
        mine.GetProperty("personDays").GetDouble().ShouldBe(12.5);

        // Plan two of the three tickets.
        var tickets = mine.GetProperty("tickets").EnumerateArray().ToList();
        tickets.Select(t => t.GetProperty("points").GetDouble()).ShouldBe([5, 3, 8]);
        var notOwner = await member.RunAsync(Planned, new { input = new { teamId, ticketId = tickets[0].GetProperty("ticketId").GetGuid(), planned = true } });
        GraphQLClient.ErrorCode(notOwner.GetProperty("setTicketPlanned")).ShouldBe("NotTeamOwner");
        await owner.RunAsync(Planned, new { input = new { teamId, ticketId = tickets[0].GetProperty("ticketId").GetGuid(), planned = true } });
        var after = (await owner.RunAsync(Planned, new { input = new { teamId, ticketId = tickets[2].GetProperty("ticketId").GetGuid(), planned = true } }))
            .GetProperty("setTicketPlanned").GetProperty("sprintPlan");
        after.GetProperty("plannedPoints").GetDouble().ShouldBe(13);

        // The members see the saved plan.
        var seen = (await member.RunAsync(Get, new { teamId, gameId })).GetProperty("sprintPlan");
        seen.GetProperty("sprintDaysSet").GetBoolean().ShouldBeTrue();
        seen.GetProperty("tickets").EnumerateArray().Count(t => t.GetProperty("planned").GetBoolean()).ShouldBe(2);

        // A game of another team is not part of this team's plan.
        var otherTeam = (await owner.RunAsync(CreateTeam, new { input = new { name = "Search" } }))
            .GetProperty("createTeam").GetProperty("team").GetProperty("id").GetGuid();
        var wrong = (await owner.RunAsync(Get, new { teamId = otherTeam, gameId })).GetProperty("sprintPlan");
        wrong.GetProperty("code").GetString().ShouldBe("SprintNotInTeam");

        // No planned ticket is linked to Jira, so there is nothing to move.
        var move = await owner.RunAsync($"mutation($input: MovePlanToJiraSprintInput!) {{ movePlanToJiraSprint(input: $input) {{ jiraSprintMoved {{ moved }} {Errors} }} }}",
            new { input = new { teamId, gameId, sprintId = 1L } });
        GraphQLClient.ErrorCode(move.GetProperty("movePlanToJiraSprint")).ShouldBe("NothingPlanned");
    }
}
