namespace OpenPlanningPoker.GameEngine.Domain.GameSettings;

public static class GameSettingsQueryableExtensions
{
    public static IQueryable<GameSettings> QueryByGame(this IQueryable<GameSettings> query, Guid gameId) =>
        query.Where(x => x.GameId == gameId);
}