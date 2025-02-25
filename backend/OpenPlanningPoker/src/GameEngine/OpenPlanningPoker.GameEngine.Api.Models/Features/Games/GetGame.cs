namespace OpenPlanningPoker.GameEngine.Api.Models.Features.Games;

public sealed record GetGameResponse(Guid Id, string Name, string Description);
public sealed record GetGameQuery(Guid GameId);
