namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer
{
    [QueryType]
    public class GamePlayerQueries
    {
        /// <summary>
        /// Returns game with participants
        /// </summary>
        public async Task<ApiCollection<GamePlayer>> GetGamePlayersAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid gameId,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new ListPlayersQuery(gameId), cancellationToken);
            return mapper.Map<ApiCollection<GamePlayer>>(result);
        }
    }
}
