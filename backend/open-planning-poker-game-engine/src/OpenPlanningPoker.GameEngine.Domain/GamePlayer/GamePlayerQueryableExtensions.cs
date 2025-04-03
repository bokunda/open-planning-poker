namespace OpenPlanningPoker.GameEngine.Domain.GamePlayer;

public static class GamePlayerQueryableExtensions
{
    public static IQueryable<GamePlayer> QueryByGame(this IQueryable<GamePlayer> query, Guid gameId) => 
        query.Where(x => x.GameId == gameId);

    public static IQueryable<GamePlayer> QueryByPlayer(this IQueryable<GamePlayer> query, Guid playerId) =>
        query.Where(x => x.PlayerId == playerId);
}