namespace OpenPlanningPoker.GameEngine.Domain.Tickets.Events;

/// <summary>A ticket got a final estimate (OPP-160); <paramref name="WasOpen"/> is false when an estimate was changed.</summary>
public sealed record TicketEstimatedDomainEvent(Guid TicketId, Guid GameId, bool WasOpen) : IDomainEvent;
