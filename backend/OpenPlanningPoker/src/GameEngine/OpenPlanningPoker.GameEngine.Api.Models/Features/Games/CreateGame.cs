namespace OpenPlanningPoker.GameEngine.Api.Models.Features.Games;

public sealed record CreateGameResponse(Guid Id, string Name, string Description);
public sealed record CreateGameCommand(string Name, string Description);
