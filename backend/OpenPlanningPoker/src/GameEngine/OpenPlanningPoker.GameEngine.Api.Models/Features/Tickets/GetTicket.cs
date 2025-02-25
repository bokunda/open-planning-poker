namespace OpenPlanningPoker.GameEngine.Api.Models.Features.Tickets;

public sealed record GetTicketResponse(Guid Id, Guid GameId, string Name, string Description);
public sealed record GetTicketQuery(Guid TicketId);
