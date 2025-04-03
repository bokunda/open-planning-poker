namespace OpenPlanningPoker.GameEngine.Api.Models.Features.GameSettings;

public sealed record UpdateGameSettingsResponse(Guid Id, Guid GameId, int VotingTime, bool IsBreakAllowed);
public sealed record UpdateGameSettingsCommand(Guid Id, Guid GameId, int VotingTime, bool IsBreakAllowed);
