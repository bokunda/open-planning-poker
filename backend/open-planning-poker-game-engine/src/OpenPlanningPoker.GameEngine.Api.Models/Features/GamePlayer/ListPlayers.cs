namespace OpenPlanningPoker.GameEngine.Api.Models.Features.GamePlayer;

public sealed record ListPlayersItem(Guid Id, string Name);
public sealed record ListPlayersQuery(Guid GameId);
    