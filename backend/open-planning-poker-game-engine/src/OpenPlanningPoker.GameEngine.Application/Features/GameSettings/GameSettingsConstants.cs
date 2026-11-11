namespace OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

public class GameSettingsConstants
{
    public const string FibonacciDeckSetup = "0,0.5,1,2,3,5,8,13,❓,⏸️";

    public const int MaxDeckCards = 30;
    public const int MaxCardLength = 10;

    public static string[] ParseDeck(string deckSetup) =>
        deckSetup.Split(',', StringSplitOptions.TrimEntries);

    /// <summary>Comma-separated, 2-30 cards, each 1-10 characters, no duplicates.</summary>
    public static bool IsValidDeck(string? deckSetup)
    {
        if (string.IsNullOrWhiteSpace(deckSetup))
        {
            return false;
        }

        var cards = ParseDeck(deckSetup);
        return cards.Length is >= 2 and <= MaxDeckCards
            && cards.All(card => card.Length is > 0 and <= MaxCardLength)
            && cards.Distinct().Count() == cards.Length;
    }
}
