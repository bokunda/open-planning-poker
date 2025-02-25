namespace OpenPlanningPoker.GameEngine.Api.Models.Features.Tickets;

public sealed record ImportTicketItem(string Name, string Description);
public sealed record ImportTicketsCommand(Guid GameId, ICollection<ImportTicketItem> Tickets);
