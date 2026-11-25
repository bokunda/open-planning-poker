using System.Text;

namespace OpenPlanningPoker.GameEngine.Application.Features.Games.Report;

/// <summary>
/// Text people typed, made safe for the PDF fonts (Red Hat Text with Lato as fallback). Since QuestPDF 2026.9 a single glyph
/// missing from the fonts fails the whole document, and names, tickets and retro cards often hold emoji or symbols.
/// Those are left out; letters, digits and the usual punctuation of Latin, Greek and Cyrillic scripts stay.
/// </summary>
public static class PdfText
{
    public static string Clean(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
        var sb = new StringBuilder(text.Length);
        var dropped = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Supported(rune.Value))
            {
                sb.Append(rune.ToString());
            }
            else
            {
                dropped = true;
            }
        }

        if (!dropped) return text;
        // Removing an emoji between two words leaves two spaces.
        var cleaned = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), " {2,}", " ").Trim();
        return cleaned;
    }

    /// <summary>
    /// Basic Latin to Cyrillic Supplement, Latin Extended Additional, General Punctuation (without the invisible joiners),
    /// and the currency and letterlike signs both fonts carry.
    /// </summary>
    private static bool Supported(int codePoint) => codePoint switch
    {
        '\n' or '\r' or '\t' => true,
        < 0x20 => false,
        < 0x0530 => true,
        >= 0x1E00 and < 0x2000 => true,
        0x200B or 0x200C or 0x200D or 0x2060 => false,
        >= 0x2000 and < 0x2070 => true,
        >= 0x20A0 and < 0x20C0 => true,
        0x2116 or 0x2122 => true,
        _ => false,
    };
}
