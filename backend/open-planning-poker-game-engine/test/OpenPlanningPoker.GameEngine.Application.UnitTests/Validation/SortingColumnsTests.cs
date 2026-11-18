using OpenPlanningPoker.GameEngine.Application.Features.Sorting;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Validation;

public class SortingColumnsTests
{
    [Fact]
    public void Columns_SkipUnsureAndBreakCards()
    {
        SortingBoard.Columns("0,1,2,3,5,8,13,?,☕").ShouldBe(["0", "1", "2", "3", "5", "8", "13"]);
        SortingBoard.Columns("0,0.5,1,2,3,5,8,13,❓,⏸️").ShouldBe(["0", "0.5", "1", "2", "3", "5", "8", "13"]);
    }

    [Fact]
    public void Columns_KeepNonNumericSizes_InDeckOrder()
    {
        SortingBoard.Columns("XS, S, M, L, XL, ⏸️").ShouldBe(["XS", "S", "M", "L", "XL"]);
    }
}
