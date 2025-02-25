namespace OpenPlanningPoker.GameEngine.Api.Models.Features.GameSettings;

public sealed record GetGameSettingsResponse(Guid Id, Guid GameId, int VotingTime, bool IsBreakAllowed);
public sealed record GetGameSettingsQuery(Guid GameId);