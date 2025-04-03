namespace OpenPlanningPoker.GameEngine.Api.Models.Features.GamePlayer;

public sealed record LeaveGameResponse;
public sealed record LeaveGameCommand(Guid GameId, Guid UserId);
