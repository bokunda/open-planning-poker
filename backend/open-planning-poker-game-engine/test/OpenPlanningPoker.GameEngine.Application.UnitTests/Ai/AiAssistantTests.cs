using System.Text.Json;
using OpenPlanningPoker.GameEngine.Application.Features.Facilitation;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Ai;

public class AiAssistantTests
{
    private sealed class FakeModel(string? answer, bool enabled = true) : IAiModel
    {
        public bool Enabled => enabled;
        public string? LastPrompt { get; private set; }
        public string? LastSystem { get; private set; }

        public Task<string?> CompleteJsonAsync(string system, string prompt, IReadOnlyDictionary<string, JsonElement> schema, int maxTokens, CancellationToken cancellationToken)
        {
            LastSystem = system;
            LastPrompt = prompt;
            return Task.FromResult(answer);
        }
    }

    private static Ticket NewTicket() => Ticket.Create(Guid.NewGuid(), "Login with SSO", "As a user I want to sign in with Google.");

    [Fact]
    public async Task WithoutAKey_NothingIsSent()
    {
        var model = new FakeModel("{}", enabled: false);
        var result = await new AiAssistantService(model).ClarityAsync(NewTicket(), default);
        result.Error!.Code.ShouldBe("AiNotAvailable");
        model.LastPrompt.ShouldBeNull();
    }

    [Fact]
    public async Task Clarity_KeepsAtMostThreePoints_AndNeverAsksForASize()
    {
        var model = new FakeModel("""{"points":["No acceptance criteria.","Which providers?","Logout flow?","Fourth"]}""");
        var result = await new AiAssistantService(model).ClarityAsync(NewTicket(), default);
        result.Value!.Count.ShouldBe(3);
        model.LastSystem!.ShouldContain("Never suggest a size");
        model.LastPrompt!.ShouldContain("Login with SSO");
    }

    [Fact]
    public async Task Split_ReadsTheTickets_AndFailsOnBadJson()
    {
        var ideas = await new AiAssistantService(new FakeModel("""{"tickets":[{"title":"Google sign-in","line":"Only Google."},{"title":"Account linking","line":"Link existing users."}]}"""))
            .SplitAsync(NewTicket(), default);
        ideas.Value!.Select(i => i.Title).ShouldBe(["Google sign-in", "Account linking"]);

        (await new AiAssistantService(new FakeModel("not json")).SplitAsync(NewTicket(), default)).Error!.Code.ShouldBe("AiFailed");
        (await new AiAssistantService(new FakeModel(null)).SplitAsync(NewTicket(), default)).Error!.Code.ShouldBe("AiFailed");
    }

    [Fact]
    public async Task Summary_ReplacesPlayerNames_BeforeSending()
    {
        var model = new FakeModel("""{"summary":"A player worried about the token refresh."}""");
        var result = await new AiAssistantService(model).SummaryAsync(NewTicket(),
            ["Ana thinks the refresh is hard"], ["marko: we did this before", "Ok Ana"], ["Ana", "Marko"], default);

        result.Value.ShouldBe("A player worried about the token refresh.");
        model.LastPrompt!.ShouldNotContain("Ana");
        model.LastPrompt.ShouldNotContain("arko");
        model.LastPrompt.ShouldContain("a player thinks the refresh is hard");
    }

    [Fact]
    public async Task Summary_NeedsSomethingToSummarize() =>
        (await new AiAssistantService(new FakeModel("{}")).SummaryAsync(NewTicket(), [], [], [], default)).Error!.Code.ShouldBe("AiNothingToSummarize");

    [Fact]
    public void Ticket_KeepsClarityAndSummaryShort()
    {
        var ticket = NewTicket();
        ticket.SetClarityNotes(["One", " ", "Two"]);
        ticket.ClarityNotes.ShouldBe("One\nTwo");
        ticket.SetDiscussionSummary(new string('x', 400));
        ticket.DiscussionSummary!.Length.ShouldBe(Ticket.MaxSummaryLength);
        ticket.SetDiscussionSummary("  ");
        ticket.DiscussionSummary.ShouldBeNull();
    }
}
