using System.Globalization;
using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using QuestPDF.Infrastructure;

namespace OpenPlanningPoker.GameEngine.Application.Features.Recaps;

/// <summary>
/// The Open Graph image of a shared recap (OPP-171): room name and three numbers on the brand violet, 1200×630 px.
/// Drawn with QuestPDF (already used for the PDF report), so no extra image library is needed. Never shows names.
/// </summary>
public static class RecapCard
{
    private const string Primary = "#6a3ae8";
    private const string PrimaryDeep = "#3d1f9e";
    private const string OnPrimary = "#ffffff";
    private const string OnPrimarySoft = "#ddd2ff";

    // 600×315 points at 144 dpi = 1200×630 pixels, the size Slack, Teams and LinkedIn expect.
    private const float Width = 600;
    private const float Height = 315;

    public static byte[] Render(RecapResponse recap)
    {
        ReportFonts.EnsureRegistered();
        var document = Document.Create(container => container.Page(page =>
        {
            page.Size(Width, Height);
            page.Margin(0);
            page.PageColor(PrimaryDeep);
            page.DefaultTextStyle(s => s.FontFamily(GameReportDocument.BodyFont).FontColor(OnPrimary));
            page.Content().Background(Primary).Padding(34).Column(column =>
            {
                column.Item().Text(text =>
                {
                    text.Span("Open Planning Poker").FontFamily(GameReportDocument.DisplayFont).ExtraBold().FontSize(13);
                    text.Span("  ·  session recap").FontSize(11).FontColor(OnPrimarySoft);
                });

                column.Item().PaddingTop(22).Height(92).Text(PdfText.Clean(recap.GameName))
                    .FontFamily(GameReportDocument.DisplayFont).ExtraBold().FontSize(34).LineHeight(1.1f).ClampLines(2);

                column.Item().PaddingTop(16).Row(row =>
                {
                    Stat(row, recap.TicketsEstimated.ToString(CultureInfo.InvariantCulture), recap.TicketsEstimated == 1 ? "ticket estimated" : "tickets estimated");
                    row.ConstantItem(14);
                    Stat(row, FormatPoints(recap.TotalPoints), "points in total");
                    row.ConstantItem(14);
                    Stat(row, recap.ConsensusRate is { } rate ? $"{Math.Round(rate * 100):0}%" : "—", "consensus");
                });

                column.Item().PaddingTop(14).AlignRight().Text("app.openplanningpoker.com").FontSize(10).FontColor(OnPrimarySoft);
            });
        }));

        return document.GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = 144 }).First();
    }

    private static void Stat(RowDescriptor row, string value, string label) =>
        row.RelativeItem().Background(PrimaryDeep).CornerRadius(10).PaddingVertical(10).PaddingHorizontal(14).Column(c =>
        {
            c.Item().Text(value).FontFamily(GameReportDocument.DisplayFont).ExtraBold().FontSize(26);
            c.Item().Text(label).FontSize(10.5f).FontColor(OnPrimarySoft);
        });

    public static string FormatPoints(double points) =>
        points % 1 == 0 ? points.ToString("0", CultureInfo.InvariantCulture) : points.ToString("0.#", CultureInfo.InvariantCulture);
}
