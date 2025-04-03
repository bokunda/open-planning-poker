namespace OpenPlanningPoker.GameEngine.Domain.Votes.Events;

public sealed record UpdateVoteDomainEvent(Guid VoteId) : IDomainEvent;