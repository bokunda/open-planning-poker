namespace OpenPlanningPoker.GameEngine.Api.Models.Features.GamePlayer;

public sealed record JoinGameResponse;
public sealed record JoinGameCommand(Guid GameId, Guid UserId);
