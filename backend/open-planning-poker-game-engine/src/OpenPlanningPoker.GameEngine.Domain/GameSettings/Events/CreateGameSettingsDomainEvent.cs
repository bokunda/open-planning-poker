namespace OpenPlanningPoker.GameEngine.Domain.GameSettings.Events;

public sealed record UpdateGameSettingsDomainEvent(Guid Id) : IDomainEvent;