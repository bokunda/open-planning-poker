namespace OpenPlanningPoker.GameEngine.Api.Models.Features.Tickets;

public sealed record UpdateTicketResponse(Guid Id, Guid GameId, string Name, string Description);
public sealed record UpdateTicketCommand(Guid TicketId, string Name, string Description);
