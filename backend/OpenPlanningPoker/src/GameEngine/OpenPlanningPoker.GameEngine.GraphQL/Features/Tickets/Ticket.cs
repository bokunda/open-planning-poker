namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ApiCollection<Vote> Votes { get; set; } = new([], 0);
    
    public async Task<ApiCollection<Vote>> GetVotes(
        [Service] ISender sender,
        [Service] IMapper mapper,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetVotesQuery(Id), cancellationToken);
        Votes = mapper.Map<ApiCollection<Vote>>(result.Value);
        return Votes;
    }

    public async Task<decimal> GetAverageVotingValue(
        [Service] ISender sender,
        [Service] IMapper mapper,
        CancellationToken cancellationToken)
    {
        Votes ??= await GetVotes(sender, mapper, cancellationToken);

        var rawVotes = Votes.Items.Select(v => v.Value);
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
        return count > 0 ? total / count : 0;
    }
}
