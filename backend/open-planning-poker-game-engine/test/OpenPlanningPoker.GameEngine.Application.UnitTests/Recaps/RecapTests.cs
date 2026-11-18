using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using OpenPlanningPoker.GameEngine.Application.Features.Recaps;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Recaps;

public class RecapTokenTests
{
    [Fact]
    public void NewToken_IsUrlSafe_WellFormed_AndRandom()
    {
        var tokens = Enumerable.Range(0, 50).Select(_ => Recap.NewToken()).ToList();
        tokens.ShouldAllBe(t => Recap.IsWellFormed(t));
        tokens.Distinct().Count().ShouldBe(50);
        tokens.ShouldAllBe(t => !t.Contains('+') && !t.Contains('/') && !t.Contains('='));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("abcdefghijklmnopqrstu!")]
    [InlineData("abcdefghijklmnopqrstuvw")]
    public void Other_Strings_AreNotLookedUp(string? token) => Recap.IsWellFormed(token).ShouldBeFalse();

    [Theory]
    [InlineData(13, "13")]
    [InlineData(10.5, "10.5")]
    [InlineData(0, "0")]
    public void Points_AreShortOnTheCard(double points, string expected) => RecapCard.FormatPoints(points).ShouldBe(expected);
}

public class RecapCardTests
{
    public RecapCardTests() => QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    [Fact]
    public void Card_IsA1200x630Png()
    {
        var recap = new RecapResponse("Sprint 7 — a rather long planning session name that needs two lines", DateTimeOffset.UtcNow,
            [new RecapTicketItem("Login", "OPP-1", "5", VoteSpread.Consensus, 1, 2.5)], 1, 0, 5, 1, 12, 4, []);

        var png = RecapCard.Render(recap);

        png.Take(8).ShouldBe(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        // IHDR: width and height as big-endian integers at bytes 16–23.
        static int At(byte[] b, int i) => (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];
        At(png, 16).ShouldBe(1200);
        At(png, 20).ShouldBe(630);
    }
}
