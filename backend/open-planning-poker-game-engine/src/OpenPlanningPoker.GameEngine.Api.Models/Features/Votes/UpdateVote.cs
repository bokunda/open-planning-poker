namespace OpenPlanningPoker.GameEngine.Api.Models.Features.Votes;

public sealed record UpdateVoteResponse(Guid Id, Guid PlayerId, Guid TicketId, int Value);
public sealed record UpdateVoteCommand(Guid Id, int Value);
