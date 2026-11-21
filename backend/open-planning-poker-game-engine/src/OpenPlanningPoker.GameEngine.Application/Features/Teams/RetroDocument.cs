using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using OpenPlanningPoker.GameEngine.Application.Localization;
using QuestPDF.Infrastructure;
using Unit = QuestPDF.Infrastructure.Unit;

namespace OpenPlanningPoker.GameEngine.Application.Features.Teams;

/// <summary>The retro as a PDF (OPP-168): one section per column with its groups, cards and votes, then the action items.</summary>
public sealed class RetroDocument(RetroService.RetroExport source) : IDocument
{
    private readonly RetroService.RetroExport export = source;

    private static string C(string? text) => PdfText.Clean(text);

    private const string Primary = "#6a3ae8";
    private const string PrimarySoft = "#f1ecff";
    private const string Text = "#18152b";
    private const string TextMuted = "#6b6687";
    private const string Border = "#e2dfec";

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{C(export.SprintName)} — {ServerText.Get("retro.title")}",
        Author = "Open Planning Poker",
        Creator = "Open Planning Poker (openplanningpoker.com)",
    };

    public static byte[] Render(RetroService.RetroExport export) => new RetroDocument(export).GeneratePdf();

    public void Compose(IDocumentContainer container)
    {
        ReportFonts.EnsureRegistered();
        var titles = RetroService.ColumnTitles(export.Retro.Template);
        var totals = export.Votes.GroupBy(v => v.CardId).ToDictionary(g => g.Key, g => g.Count());
        var items = export.Items.Where(i => i.RetroId == export.Retro.Id).ToList();

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(14, Unit.Millimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(style => style.FontFamily(GameReportDocument.BodyFont).FontSize(9.5f).FontColor(Text).LineHeight(1.35f));

            page.Header().BorderBottom(1).BorderColor(Border).PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Open Planning ").FontFamily(GameReportDocument.DisplayFont).ExtraBold().FontSize(12);
                    text.Span("Poker").FontFamily(GameReportDocument.DisplayFont).ExtraBold().FontSize(12).FontColor(Primary);
                });
                row.AutoItem().AlignMiddle().Text(ServerText.Get("retro.title")).FontSize(9).SemiBold().FontColor(TextMuted);
            });

            page.Content().PaddingVertical(10).Column(column =>
            {
                column.Spacing(10);
                column.Item().Text(C(export.SprintName)).FontFamily(GameReportDocument.DisplayFont).ExtraBold().FontSize(18);

                for (var c = 0; c < titles.Count; c++)
                {
                    var heads = export.Cards.Where(card => card.Column == c && card.GroupId is null)
                        .OrderByDescending(card => totals.GetValueOrDefault(card.Id)).ToList();
                    var index = c;
                    column.Item().Column(section =>
                    {
                        section.Spacing(4);
                        section.Item().Text(titles[index]).FontSize(12).Bold().FontColor(Primary);
                        if (heads.Count == 0) section.Item().Text("—").FontColor(TextMuted);
                        foreach (var head in heads)
                        {
                            var members = export.Cards.Where(card => card.GroupId == head.Id).ToList();
                            var count = totals.GetValueOrDefault(head.Id);
                            section.Item().Border(1).BorderColor(Border).Padding(6).Column(card =>
                            {
                                card.Item().Row(row =>
                                {
                                    var title = row.RelativeItem().Text(C(members.Count > 0 ? head.GroupTitle ?? head.Text : head.Text));
                                    if (members.Count > 0) title.SemiBold();
                                    if (count > 0)
                                    {
                                        row.AutoItem().Background(PrimarySoft).PaddingHorizontal(5).PaddingVertical(1)
                                            .Text(ServerText.Get("retro.votes", count)).FontSize(8.5f).FontColor(Primary).SemiBold();
                                    }
                                });
                                foreach (var member in head.GroupTitle is null ? members : [head, .. members])
                                {
                                    card.Item().PaddingLeft(8).Text($"• {C(member.Text)}").FontColor(TextMuted);
                                }
                            });
                        }
                    });
                }

                column.Item().Column(section =>
                {
                    section.Spacing(4);
                    section.Item().Text(ServerText.Get("retro.actionItems")).FontSize(12).Bold().FontColor(Primary);
                    if (items.Count == 0) section.Item().Text("—").FontColor(TextMuted);
                    foreach (var item in items)
                    {
                        var owner = item.OwnerId is { } id && export.NameOf(id) is { } name ? $" — {C(name)}" : string.Empty;
                        section.Item().Text($"{(item.Done ? "[x]" : "[ ]")} {C(item.Text)}{owner}");
                    }
                });
            });

            page.Footer().AlignRight().Text(text =>
            {
                text.Span("openplanningpoker.com · ").FontSize(8).FontColor(TextMuted);
                text.CurrentPageNumber().FontSize(8).FontColor(TextMuted);
            });
        });
    }
}
