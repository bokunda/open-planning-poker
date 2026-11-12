namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool VotesRevealed { get; set; }

    /// <summary>The estimate the host chose after the reveal; null until one is chosen.</summary>
    public string? FinalEstimate { get; set; }

    /// <summary>Place of the ticket in the game's list (0 first).</summary>
    public int Position { get; set; }
    public ApiCollection<Vote> Votes { get; set; } = new([], 0);
    
    public async Task<ApiCollection<Vote>> GetVotes(
        [Service] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetVotesQuery(Id), cancellationToken);
        return result.Value.ToGraphQL(item => item.ToGraphQL());
    }

    public async Task<decimal> GetAverageVotingValue(
        [Service] ISender sender,
        CancellationToken cancellationToken)
    {
        if (!VotesRevealed)
        {
            return 0;
        }

        var votes = await GetVotes(sender, cancellationToken);

        var rawVotes = votes.Items.Select(v => v.Value);
        var total = 0M;
        var count = 0;

        foreach (var vote in rawVotes)
        {
            if (!decimal.TryParse(vote, out var parsedVote)) 
            { 
                continue;
            }

            total += parsedVote;
            count++;
        }
        return count > 0
            ? decimal.Round(total /count, 2) 
            : 0;
    }
}
