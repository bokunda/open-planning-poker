namespace OpenPlanningPoker.GameEngine.Domain.Games.Events;

public sealed record CreateGameDomainEvent(Guid GameId) : IDomainEvent;