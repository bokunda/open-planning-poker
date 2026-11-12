using System.Globalization;
using System.Reflection;
using QuestPDF.Infrastructure;
using Unit = QuestPDF.Infrastructure.Unit;

namespace OpenPlanningPoker.GameEngine.Application.Features.Games.Report;

public sealed record ReportVote(string Player, string Value);

/// <param name="FinalEstimate">The estimate the host chose; when null the report shows the most common card.</param>
public sealed record ReportTicket(string Name, string Description, bool Revealed, IReadOnlyList<ReportVote> Votes, string? FinalEstimate = null);

public sealed record ReportModel(
    string GameName,
    string Description,
    DateTimeOffset CreatedOn,
    DateTimeOffset GeneratedOn,
    IReadOnlyList<ReportTicket> Tickets);

/// <summary>
/// The game report PDF, styled like the app and the website: brand violet, Figtree for text,
/// Bricolage Grotesque for headings, summary tiles and one card per ticket.
/// </summary>
public sealed class GameReportDocument(ReportModel model) : IDocument
{
    // Colors from the app's design tokens (styles/_variables.scss, light theme).
    private const string Primary = "#6a3ae8";
    private const string PrimarySoft = "#f1ecff";
    private const string Text = "#18152b";
    private const string TextSecondary = "#524d6b";
    private const string TextMuted = "#6b6687";
    private const string Border = "#e2dfec";
    private const string Surface = "#f7f6fb";
    private const string Consensus = "#0f6b46";
    private const string ConsensusSoft = "#e3f4ec";
    private const string Spread = "#8a4a0c";
    private const string SpreadSoft = "#fdf0e1";

    private const string BodyFont = "Figtree";
    private const string DisplayFont = "Bricolage";

    private static readonly Lazy<byte[]> Logo = new(() => ReadResource("logo.png"));

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{model.GameName} — planning poker report",
        Author = "Open Planning Poker",
        Creator = "Open Planning Poker (openplanningpoker.com)",
        CreationDate = model.GeneratedOn,
    };

    public void Compose(IDocumentContainer container)
    {
        ReportFonts.EnsureRegistered();
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(14, Unit.Millimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(style => style.FontFamily(BodyFont).FontSize(9.5f).FontColor(Text).LineHeight(1.35f));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(10).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.BorderBottom(1).BorderColor(Border).PaddingBottom(8).Row(row =>
        {
            row.ConstantItem(22).Height(22).Image(Logo.Value).FitArea();
            row.ConstantItem(8);
            row.RelativeItem().AlignMiddle().Text(text =>
            {
                text.Span("Open Planning ").FontFamily(DisplayFont).ExtraBold().FontSize(12);
                text.Span("Poker").FontFamily(DisplayFont).ExtraBold().FontSize(12).FontColor(Primary);
            });
            row.AutoItem().AlignMiddle().Text("Game report").FontSize(9).SemiBold().FontColor(TextMuted);
        });
    }

    private void ComposeContent(IContainer container)
    {
        var tickets = model.Tickets.Select(t => (Ticket: t, Stats: TicketStats.From(t.Revealed, [.. t.Votes.Select(v => v.Value)]))).ToList();
        var revealed = tickets.Count(t => t.Ticket.Revealed);
        var players = model.Tickets.SelectMany(t => t.Votes).Select(v => v.Player).Distinct().Count();
        var averages = tickets.Where(t => t.Stats.Average.HasValue).Select(t => t.Stats.Average!.Value).ToList();

        container.Column(column =>
        {
            column.Spacing(12);

            // Title block
            column.Item().Column(title =>
            {
                title.Item().Text(model.GameName).FontFamily(DisplayFont).ExtraBold().FontSize(24).FontColor(Text);
                if (!string.IsNullOrWhiteSpace(model.Description))
                {
                    title.Item().PaddingTop(2).Text(model.Description).FontSize(10.5f).FontColor(TextSecondary);
                }
                title.Item().PaddingTop(4).Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(8.5f).FontColor(TextMuted));
                    text.Span($"Created {Date(model.CreatedOn)}");
                    text.Span("   ·   ");
                    text.Span($"Exported {Date(model.GeneratedOn)} UTC");
                });
            });

            // Summary tiles
            column.Item().Row(row =>
            {
                row.Spacing(8);
                Tile(row.RelativeItem(), "Tickets", tickets.Count.ToString(CultureInfo.InvariantCulture));
                Tile(row.RelativeItem(), "Revealed", $"{revealed} of {tickets.Count}");
                Tile(row.RelativeItem(), "Players", players.ToString(CultureInfo.InvariantCulture));
                Tile(row.RelativeItem(), "Average estimate", averages.Count > 0 ? Number(averages.Average()) : "–");
            });

            column.Item().PaddingTop(4).Text("Tickets").FontFamily(DisplayFont).ExtraBold().FontSize(14);

            if (tickets.Count == 0)
            {
                column.Item().Text("No tickets in this game yet.").FontColor(TextMuted);
            }

            foreach (var (ticket, stats) in tickets)
            {
                column.Item().ShowEntire().Element(c => TicketCard(c, ticket, stats));
            }
        });
    }

    private static void Tile(IContainer container, string label, string value)
    {
        container.Background(Surface).Border(1).BorderColor(Border).CornerRadius(10).Padding(10).Column(column =>
        {
            column.Item().Text(label.ToUpperInvariant()).FontSize(7).SemiBold().LetterSpacing(0.08f).FontColor(TextMuted);
            column.Item().PaddingTop(2).Text(value).FontFamily(DisplayFont).ExtraBold().FontSize(16).FontColor(Text);
        });
    }

    private static void TicketCard(IContainer container, ReportTicket ticket, TicketStats stats)
    {
        container.Border(1).BorderColor(Border).CornerRadius(12).Padding(12).Column(column =>
        {
            column.Spacing(6);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(ticket.Name).FontFamily(DisplayFont).ExtraBold().FontSize(12).FontColor(Text);
                row.AutoItem().AlignMiddle().Element(c => SpreadPill(c, stats.Spread));
            });

            if (!string.IsNullOrWhiteSpace(ticket.Description))
            {
                column.Item().Text(ticket.Description).FontSize(8.5f).FontColor(TextSecondary);
            }

            if (stats.Spread is VoteSpread.NotRevealed or VoteSpread.NoVotes)
            {
                column.Item().Text(stats.Spread == VoteSpread.NotRevealed ? "The cards were not revealed." : "Nobody voted.")
                    .FontSize(9).FontColor(TextMuted);
                return;
            }

            // Estimate, average, median, votes
            column.Item().Row(row =>
            {
                row.Spacing(18);
                Stat(row.AutoItem(), ticket.FinalEstimate is null ? "Estimate" : "Final estimate", ticket.FinalEstimate ?? stats.Estimate ?? "–", emphasize: true);
                Stat(row.AutoItem(), "Average", stats.Average is { } a ? Number(a) : "–");
                Stat(row.AutoItem(), "Median", stats.Median is { } m ? Number(m) : "–");
                Stat(row.AutoItem(), "Votes", ticket.Votes.Count.ToString(CultureInfo.InvariantCulture));
            });

            // Distribution bars
            var max = stats.Distribution.Max(d => d.Count);
            column.Item().PaddingTop(2).Column(bars =>
            {
                bars.Spacing(3);
                foreach (var entry in stats.Distribution)
                {
                    bars.Item().Row(row =>
                    {
                        row.ConstantItem(34).Text(entry.Value).SemiBold().FontSize(9);
                        row.RelativeItem().AlignMiddle().Height(7).Background(PrimarySoft).CornerRadius(4).Row(track =>
                        {
                            track.RelativeItem(entry.Count).Background(Primary).CornerRadius(4);
                            if (max > entry.Count) track.RelativeItem(max - entry.Count);
                        });
                        row.ConstantItem(26).AlignRight().Text($"×{entry.Count}").FontSize(8.5f).FontColor(TextMuted);
                    });
                }
            });

            // Who voted what, sorted by value like the table in the app
            column.Item().PaddingTop(2).Inlined(chips =>
            {
                chips.Spacing(4);
                foreach (var vote in ticket.Votes.OrderBy(v => SortKey(v.Value)).ThenBy(v => v.Player, StringComparer.OrdinalIgnoreCase))
                {
                    chips.Item().Background(Surface).Border(1).BorderColor(Border).CornerRadius(8)
                        .PaddingHorizontal(6).PaddingVertical(2)
                        .Text(text =>
                        {
                            text.Span(vote.Player).FontSize(8).FontColor(TextSecondary);
                            text.Span("  ");
                            text.Span(vote.Value).FontSize(8).Bold().FontColor(Primary);
                        });
                }
            });
        });
    }

    private static void Stat(IContainer container, string label, string value, bool emphasize = false)
    {
        container.Column(column =>
        {
            column.Item().Text(label).FontSize(7.5f).FontColor(TextMuted);
            column.Item().Text(value).FontFamily(DisplayFont).ExtraBold().FontSize(emphasize ? 15 : 12)
                .FontColor(emphasize ? Primary : Text);
        });
    }

    private static void SpreadPill(IContainer container, VoteSpread spread)
    {
        var (label, color, background) = spread switch
        {
            VoteSpread.Consensus => ("Consensus", Consensus, ConsensusSoft),
            VoteSpread.Close => ("Close to consensus", Consensus, ConsensusSoft),
            VoteSpread.Wide => ("Wide spread", Spread, SpreadSoft),
            VoteSpread.NoVotes => ("No votes", TextMuted, Surface),
            _ => ("Not revealed", TextMuted, Surface),
        };
        container.Background(background).CornerRadius(9).PaddingHorizontal(8).PaddingVertical(2)
            .Text(label).FontSize(7.5f).SemiBold().FontColor(color);
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Border).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Generated by ").FontSize(8).FontColor(TextMuted);
                text.Hyperlink("openplanningpoker.com", "https://openplanningpoker.com").FontSize(8).SemiBold().FontColor(Primary);
            });
            row.AutoItem().Text(text =>
            {
                text.DefaultTextStyle(s => s.FontSize(8).FontColor(TextMuted));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }

    private static string Date(DateTimeOffset value) => value.UtcDateTime.ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);

    private static string Number(double value) => value.ToString(value % 1 == 0 ? "0" : "0.0#", CultureInfo.InvariantCulture);

    private static double SortKey(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : double.MaxValue;

    internal static byte[] ReadResource(string fileName)
    {
        var assembly = typeof(GameReportDocument).Assembly;
        var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($".Report.Resources.{fileName}", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}

/// <summary>Registers the report fonts (embedded TTF files) with QuestPDF once per process.</summary>
internal static class ReportFonts
{
    private static readonly Lazy<bool> Registered = new(() =>
    {
        foreach (var file in new[] { "Figtree-400.ttf", "Figtree-600.ttf", "Figtree-700.ttf" })
        {
            QuestPDF.Drawing.FontManager.RegisterFontWithCustomName("Figtree", new MemoryStream(GameReportDocument.ReadResource(file)));
        }
        QuestPDF.Drawing.FontManager.RegisterFontWithCustomName("Bricolage", new MemoryStream(GameReportDocument.ReadResource("BricolageGrotesque-800.ttf")));
        return true;
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    public static void EnsureRegistered() => _ = Registered.Value;
}
