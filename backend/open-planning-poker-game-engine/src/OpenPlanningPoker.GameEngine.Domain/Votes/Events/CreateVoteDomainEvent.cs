namespace OpenPlanningPoker.GameEngine.Domain.Votes.Events;

public sealed record CreateVoteDomainEvent(Guid VoteId) : IDomainEvent;