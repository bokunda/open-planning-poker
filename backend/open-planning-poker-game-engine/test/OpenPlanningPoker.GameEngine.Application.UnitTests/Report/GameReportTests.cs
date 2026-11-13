using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using QuestPDF.Fluent;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Report;

public class TicketStatsTests
{
    [Fact]
    public void NotRevealed_HasNoNumbers()
    {
        var stats = TicketStats.From(revealed: false, ["5", "8"]);
        stats.Spread.ShouldBe(VoteSpread.NotRevealed);
        stats.Average.ShouldBeNull();
        stats.Distribution.ShouldBeEmpty();
    }

    [Fact]
    public void SameCard_IsConsensus()
    {
        var stats = TicketStats.From(true, ["5", "5", "5"]);
        stats.Spread.ShouldBe(VoteSpread.Consensus);
        stats.Estimate.ShouldBe("5");
        stats.Median.ShouldBe(5);
    }

    [Fact]
    public void NeighbouringValues_AreClose()
    {
        var stats = TicketStats.From(true, ["3", "5", "5", "5"]);
        stats.Spread.ShouldBe(VoteSpread.Close);
        stats.Estimate.ShouldBe("5");
        stats.Average.ShouldBe(4.5);
        stats.Median.ShouldBe(5);
        stats.Distribution.Select(d => d.Value).ShouldBe(["3", "5"]);
    }

    [Fact]
    public void FarApart_IsWideSpread()
    {
        var stats = TicketStats.From(true, ["3", "5", "13"]);
        stats.Spread.ShouldBe(VoteSpread.Wide);
        stats.Estimate.ShouldBe("5"); // all cards once: the one closest to the median
    }

    [Fact]
    public void TShirtSizes_UseMostCommonCard()
    {
        var stats = TicketStats.From(true, ["M", "L", "M"]);
        stats.Average.ShouldBeNull();
        stats.Estimate.ShouldBe("M");
        stats.Spread.ShouldBe(VoteSpread.Close);
    }
}

public class GameReportDocumentTests
{
    static GameReportDocumentTests() => QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    private static ReportModel Sample() => new(
        "Sprint 42 refinement",
        "Checkout and payments epic",
        new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 12, 10, 30, 0, TimeSpan.Zero),
        [
            new("OPP-112 Save cart between sessions", "Keep the cart for 30 days for signed-in users.", true,
                [new("Ana", "5"), new("Marko", "5"), new("Jelena", "3"), new("Stefan", "5"), new("Ivana", "5")]),
            new("OPP-115 Apple Pay at checkout", "", true,
                [new("Ana", "8"), new("Marko", "13"), new("Jelena", "8"), new("Stefan", "8"), new("Đorđe Čolić", "8")]),
            new("OPP-118 Export results to CSV", "", true,
                [new("Ana", "5"), new("Marko", "13"), new("Jelena", "5"), new("Stefan", "3"), new("Ivana", "5")]),
            new("OPP-120 Gift cards", "", true, [new("Ana", "M"), new("Marko", "M"), new("Jelena", "M")]),
            new("OPP-121 Discount codes", "", false, []),
        ]);

    [Fact]
    public void GeneratesPdf()
    {
        var pdf = new GameReportDocument(Sample()).GeneratePdf();

        pdf.Length.ShouldBeGreaterThan(10_000);
        System.Text.Encoding.ASCII.GetString(pdf, 0, 5).ShouldBe("%PDF-");

        // Set OPP_REPORT_OUT to a file path to look at the sample report.
        if (Environment.GetEnvironmentVariable("OPP_REPORT_OUT") is { Length: > 0 } path)
        {
            File.WriteAllBytes(path, pdf);
            new GameReportDocument(Sample()).GenerateImages(
                index => Path.ChangeExtension(path, null) + $"-page{index + 1}.png",
                new QuestPDF.Infrastructure.ImageGenerationSettings { RasterDpi = 110 });
        }
    }

    [Fact]
    public void RegistersTheReportFonts_UnderTheFamilyNamesTheReportUses()
    {
        new GameReportDocument(Sample()).GeneratePdf();

        var families = QuestPDF.Drawing.FontManager.GetRegisteredFonts().Select(font => font.FamilyName).ToList();
        families.ShouldContain("Figtree");
        families.ShouldContain("Bricolage Grotesque 48pt");
    }

    [Fact]
    public void GeneratesPdf_ForEmptyGame()
    {
        var empty = Sample() with { Tickets = [], Description = "" };
        new GameReportDocument(empty).GeneratePdf().Length.ShouldBeGreaterThan(5_000);
    }
}
