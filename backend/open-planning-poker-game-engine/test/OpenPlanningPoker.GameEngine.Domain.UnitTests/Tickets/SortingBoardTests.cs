using OpenPlanningPoker.GameEngine.Domain.Games;

namespace OpenPlanningPoker.GameEngine.Domain.UnitTests.Tickets;

public class SortingBoardTests
{
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Ben = Guid.NewGuid();

    [Fact]
    public void Move_PutsTicketInColumn_AndCountsIt()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");

        ticket.MoveOnBoard("5", Ana).ShouldBeTrue();

        ticket.SortColumn.ShouldBe("5");
        ticket.SortMoves.ShouldBe(1);
        ticket.SortContested.ShouldBeFalse();
    }

    [Fact]
    public void Move_ToSameColumn_IsNoOp()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        ticket.MoveOnBoard("5", Ana);

        ticket.MoveOnBoard(" 5 ", Ben).ShouldBeFalse();

        ticket.SortMoves.ShouldBe(1);
        ticket.SortContested.ShouldBeFalse();
    }

    [Fact]
    public void SamePlayerAdjusting_IsNotContested_UntilThirdMove()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        ticket.MoveOnBoard("3", Ana);
        ticket.MoveOnBoard("5", Ana);
        ticket.SortContested.ShouldBeFalse();

        ticket.MoveOnBoard("8", Ana);

        ticket.SortContested.ShouldBeTrue();
    }

    [Fact]
    public void AnotherPlayerMovingIt_IsContested()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        ticket.MoveOnBoard("3", Ana);

        ticket.MoveOnBoard("13", Ben);

        ticket.SortContested.ShouldBeTrue();
        ticket.SortColumn.ShouldBe("13");
    }

    [Fact]
    public void ClearContested_StartsCountingAgain()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        ticket.MoveOnBoard("3", Ana);
        ticket.MoveOnBoard("13", Ben);

        ticket.ClearContested();

        ticket.SortContested.ShouldBeFalse();
        ticket.SortMoves.ShouldBe(0);
        ticket.SortColumn.ShouldBe("13");
        ticket.MoveOnBoard("8", Ana);
        ticket.SortContested.ShouldBeFalse();
    }

    [Fact]
    public void Apply_SetsFinalEstimate_OnlyForSettledTickets()
    {
        var settled = Ticket.Create(Guid.NewGuid(), "Login", "");
        settled.MoveOnBoard("5", Ana);
        var contested = Ticket.Create(Guid.NewGuid(), "Search", "");
        contested.MoveOnBoard("3", Ana);
        contested.MoveOnBoard("8", Ben);
        var unsorted = Ticket.Create(Guid.NewGuid(), "Export", "");

        settled.ApplySortedEstimate().ShouldBeTrue();
        contested.ApplySortedEstimate().ShouldBeFalse();
        unsorted.ApplySortedEstimate().ShouldBeFalse();

        settled.FinalEstimate.ShouldBe("5");
        settled.EstimatedAt.ShouldNotBeNull();
        contested.FinalEstimate.ShouldBeNull();
    }

    [Fact]
    public void VotedFinalEstimate_MovesContestedTicketToItsColumn()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        ticket.MoveOnBoard("3", Ana);
        ticket.MoveOnBoard("8", Ben);
        ticket.RevealVotes();

        ticket.SetFinalEstimate("5");

        ticket.SortColumn.ShouldBe("5");
        ticket.SortContested.ShouldBeFalse();
    }

    [Fact]
    public void VotedFinalEstimate_LeavesTicketsOffTheBoardAlone()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        ticket.RevealVotes();

        ticket.SetFinalEstimate("5");

        ticket.SortColumn.ShouldBeNull();
    }
}
