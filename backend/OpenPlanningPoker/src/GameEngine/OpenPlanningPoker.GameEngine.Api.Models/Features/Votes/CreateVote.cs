namespace OpenPlanningPoker.GameEngine.Api.Models.Features.Votes;

public sealed record CreateVoteResponse(Guid Id, Guid PlayerId, Guid TicketId, int Value);
public sealed record CreateVoteCommand(Guid TicketId, int Value);
