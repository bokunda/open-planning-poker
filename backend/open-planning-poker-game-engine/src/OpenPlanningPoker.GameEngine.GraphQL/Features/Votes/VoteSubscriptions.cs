namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

[SubscriptionType]
public class VoteSubscriptions
{
    [Subscribe]
    [Topic($"{nameof(VoteMutations.CreateOrUpdateVoteAsync)}_{{{nameof(ticketId)}}}")]
    public Vote OnVoteCreatedOrUpdated(Guid ticketId, [EventMessage] Vote vote) => vote;

    [Subscribe]
    [Topic($"{nameof(VoteMutations.RevealVotesAsync)}_{{{nameof(ticketId)}}}")]
    public VotesRevealed OnVotesRevealed(Guid ticketId, [EventMessage] VotesRevealed reveal) => reveal;
}
