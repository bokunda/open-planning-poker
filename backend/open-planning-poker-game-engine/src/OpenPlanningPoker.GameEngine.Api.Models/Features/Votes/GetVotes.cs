namespace OpenPlanningPoker.GameEngine.Api.Models.Features.Votes;

public sealed record GetVotesItem(Guid Id, Guid PlayerId, int Value);
public sealed record GetVotesQuery(Guid TicketId);
