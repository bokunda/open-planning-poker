namespace OpenPlanningPoker.GameEngine.Api.Models.Features.Tickets;

public sealed record CreateTicketResponse(Guid Id, Guid GameId, string Name, string Description);
public sealed record CreateTicketCommand(Guid GameId, string Name, string Description);
