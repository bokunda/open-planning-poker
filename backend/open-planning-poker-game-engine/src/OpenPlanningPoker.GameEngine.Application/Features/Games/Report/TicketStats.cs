using System.Globalization;

namespace OpenPlanningPoker.GameEngine.Application.Features.Games.Report;

public enum VoteSpread
{
    NotRevealed,
    NoVotes,
    Consensus,
    Close,
    Wide,
}

public sealed record VoteCount(string Value, int Count);

/// <summary>Numbers shown for one ticket in the report. Mirrors the results panel in the app.</summary>
public sealed record TicketStats(
    double? Average,
    double? Median,
    string? Estimate,
    VoteSpread Spread,
    IReadOnlyList<VoteCount> Distribution)
{
    /// <summary>
    /// Consensus: everyone picked the same card. Close: two neighbouring values (numeric: the highest is at most
    /// twice the lowest). Wide: anything else. Non-numeric cards (T-shirt sizes, "?") only count towards the distribution.
    /// </summary>
    public static TicketStats From(bool revealed, IReadOnlyCollection<string> values)
    {
        if (!revealed) return new(null, null, null, VoteSpread.NotRevealed, []);
        if (values.Count == 0) return new(null, null, null, VoteSpread.NoVotes, []);

        var numbers = values
            .Select(v => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : (double?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .OrderBy(n => n)
            .ToList();

        var distribution = values
            .GroupBy(v => v)
            .Select(g => new VoteCount(g.Key, g.Count()))
            .OrderBy(c => double.TryParse(c.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : double.MaxValue)
            .ThenBy(c => c.Value, StringComparer.Ordinal)
            .ToList();

        double? average = numbers.Count > 0 ? numbers.Average() : null;
        double? median = numbers.Count switch
        {
            0 => null,
            var c when c % 2 == 1 => numbers[c / 2],
            var c => (numbers[c / 2 - 1] + numbers[c / 2]) / 2,
        };

        var distinct = distribution.Count;
        VoteSpread spread;
        if (distinct == 1) spread = VoteSpread.Consensus;
        else if (numbers.Count == values.Count && numbers[0] > 0 && numbers[^1] <= numbers[0] * 2) spread = VoteSpread.Close;
        else if (numbers.Count != values.Count && distinct == 2) spread = VoteSpread.Close;
        else spread = VoteSpread.Wide;

        // The estimate is a card, not a computed number: the most common card. On a tie, the card closest
        // to the median wins (then the higher one), so 3 / 5 / 13 gives 5.
        var estimate = distribution
            .OrderByDescending(c => c.Count)
            .ThenBy(c => median is { } med && TryNumber(c.Value, out var n) ? Math.Abs(n - med) : double.MaxValue)
            .ThenByDescending(c => TryNumber(c.Value, out var n) ? n : double.MinValue)
            .First().Value;

        return new(average, median, estimate, spread, distribution);
    }

    private static bool TryNumber(string value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
}
