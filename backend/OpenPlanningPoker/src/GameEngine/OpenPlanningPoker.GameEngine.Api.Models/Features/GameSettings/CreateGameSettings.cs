namespace OpenPlanningPoker.GameEngine.Api.Models.Features.GameSettings;

public sealed record CreateGameSettingsResponse(Guid Id, Guid GameId, int VotingTime, bool IsBreakAllowed);
public sealed record CreateGameSettingsCommand(Guid GameId, int VotingTime, bool IsBreakAllowed);
