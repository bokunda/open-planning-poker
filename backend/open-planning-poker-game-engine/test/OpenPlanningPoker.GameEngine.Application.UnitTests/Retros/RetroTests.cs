using OpenPlanningPoker.GameEngine.Application.Features.Teams;
using OpenPlanningPoker.GameEngine.Domain.Teams;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Retros;

public class RetroTests
{
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    private static Retro NewRetro(string template = "went-well")
    {
        var retro = Retro.Create(Guid.NewGuid(), Guid.NewGuid(), template);
        retro.SetCreated(DateTimeOffset.UtcNow, Ana);
        return retro;
    }

    private static RetroCard Card(Retro retro, Guid author, int column, string text)
    {
        var card = RetroCard.Create(retro.Id, column, text);
        card.SetCreated(DateTimeOffset.UtcNow, author);
        return card;
    }

    [Fact]
    public void Phases_OnlyMoveForward()
    {
        var retro = NewRetro();
        retro.MoveTo(RetroPhase.Voting).ShouldBeTrue();
        retro.MoveTo(RetroPhase.Grouping).ShouldBeFalse();
        retro.Phase.ShouldBe(RetroPhase.Voting);
    }

    [Fact]
    public void Settings_ChangeOnlyWhileWriting_AndDotsAreClamped()
    {
        var retro = NewRetro();
        retro.UpdateSettings(true, 99).ShouldBeTrue();
        retro.DotsPerPerson.ShouldBe(Retro.MaxDots);
        retro.MoveTo(RetroPhase.Grouping);
        retro.UpdateSettings(false, 3).ShouldBeFalse();
        retro.ShowAuthors.ShouldBeTrue();
    }

    [Fact]
    public void WhileWriting_OthersSeeOnlyCounts()
    {
        var retro = NewRetro();
        var cards = new[] { Card(retro, Ana, 0, "Pairing"), Card(retro, Bob, 0, "CI"), Card(retro, Bob, 2, "Demo day") };

        var bobView = RetroService.Build(retro, cards, [], [], Bob);

        bobView.Cards.Select(c => c.Text).ShouldBe(["CI", "Demo day"], ignoreOrder: true);
        bobView.Cards.ShouldAllBe(c => c.Mine);
        bobView.CountPerColumn.ShouldBe([2, 0, 1]);
    }

    [Fact]
    public void AfterReveal_EveryoneSeesAll_AuthorsOnlyWhenShown()
    {
        var retro = NewRetro();
        var cards = new[] { Card(retro, Ana, 0, "Pairing"), Card(retro, Bob, 1, "CI") };
        retro.MoveTo(RetroPhase.Grouping);

        var view = RetroService.Build(retro, cards, [], [], Bob);
        view.Cards.Count.ShouldBe(2);
        view.Cards.ShouldAllBe(c => c.AuthorId == null);

        var named = NewRetro();
        named.UpdateSettings(true, 3);
        named.MoveTo(RetroPhase.Grouping);
        RetroService.Build(named, [Card(named, Ana, 0, "x")], [], [], Bob).Cards.Single().AuthorId.ShouldBe(Ana);
    }

    [Fact]
    public void Votes_StayHidden_UntilTheResults_ThenCardsSortByVotes()
    {
        var retro = NewRetro();
        var low = Card(retro, Ana, 0, "Low");
        var high = Card(retro, Ana, 1, "High");
        var votes = new[]
        {
            RetroVote.Create(retro.Id, high.Id, Ana), RetroVote.Create(retro.Id, high.Id, Bob), RetroVote.Create(retro.Id, low.Id, Bob),
        };
        retro.MoveTo(RetroPhase.Voting);

        var voting = RetroService.Build(retro, [low, high], votes, [], Bob);
        voting.Cards.ShouldAllBe(c => c.Votes == null);
        voting.Cards.Single(c => c.Id == high.Id).MyVotes.ShouldBe(1);
        voting.DotsLeft.ShouldBe(1);

        retro.MoveTo(RetroPhase.Discussing);
        var results = RetroService.Build(retro, [low, high], votes, [], Bob);
        results.Cards.Select(c => c.Text).ShouldBe(["High", "Low"]);
        results.Cards[0].Votes.ShouldBe(2);
    }

    [Fact]
    public void OpenActionItemsOfEarlierRetros_ComeBack()
    {
        var earlier = NewRetro();
        var teamId = earlier.TeamId;
        var open = RetroActionItem.Create(teamId, earlier.Id, null, "Fix flaky tests", Bob);
        open.SetCreated(DateTimeOffset.UtcNow.AddDays(-14), Ana);
        var closed = RetroActionItem.Create(teamId, earlier.Id, null, "Old one", null);
        closed.SetCreated(DateTimeOffset.UtcNow.AddDays(-14), Ana);
        closed.SetDone(true);
        closed.GetType().GetProperty(nameof(RetroActionItem.DoneOn))!.SetValue(closed, DateTimeOffset.UtcNow.AddDays(-7));

        var now = Retro.Create(teamId, Guid.NewGuid(), "4ls");
        now.SetCreated(DateTimeOffset.UtcNow, Ana);
        var view = RetroService.Build(now, [], [], [open, closed], Ana);

        view.Columns.ShouldBe(4);
        view.CarriedOver.Select(i => i.Text).ShouldBe(["Fix flaky tests"]);
        view.ActionItems.ShouldBeEmpty();
    }

    [Fact]
    public void Markdown_ListsGroupsWithVotes_AndActionItems()
    {
        var retro = NewRetro("start-stop-continue");
        var head = Card(retro, Ana, 0, "Pair on reviews");
        head.SetGroupTitle("Reviews");
        var member = Card(retro, Bob, 0, "Smaller PRs");
        member.JoinGroup(head.Id, 0);
        var votes = new[] { RetroVote.Create(retro.Id, head.Id, Ana), RetroVote.Create(retro.Id, head.Id, Bob) };
        var item = RetroActionItem.Create(retro.TeamId, retro.Id, head.Id, "Review within a day", Bob);

        var md = RetroService.Markdown("Sprint 12", retro, [head, member], votes, [item], id => id == Bob ? "Bob" : null);

        md.ShouldContain("# Retro: Sprint 12");
        md.ShouldContain("## Start");
        md.ShouldContain("- **Reviews** (2 votes)");
        md.ShouldContain("  - Pair on reviews");
        md.ShouldContain("  - Smaller PRs");
        md.ShouldContain("- [ ] Review within a day — Bob");
    }

    [Fact]
    public void Summary_NamesTheTopTopics()
    {
        var retro = NewRetro();
        var card = Card(retro, Ana, 1, "Flaky CI");
        var text = RetroService.Summary("Sprint 3", retro, [card], [RetroVote.Create(retro.Id, card.Id, Ana)], [], _ => null, "https://x/retro/1");
        text.ShouldContain("Sprint 3");
        text.ShouldContain("Flaky CI (1)");
        text.ShouldEndWith("https://x/retro/1");
    }
}

public class RetroDocumentTests
{
    public RetroDocumentTests() => QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    [Fact]
    public void Pdf_IsRendered()
    {
        var retro = Retro.Create(Guid.NewGuid(), Guid.NewGuid(), "mad-sad-glad");
        var card = RetroCard.Create(retro.Id, 2, "Shipped recaps");
        var export = new RetroService.RetroExport("Sprint 9", retro, [card], [], [RetroActionItem.Create(retro.TeamId, retro.Id, null, "Keep it up", null)],
            new Dictionary<Guid, string>());

        var pdf = RetroDocument.Render(export);

        pdf.Take(4).ShouldBe("%PDF"u8.ToArray());
    }
}
