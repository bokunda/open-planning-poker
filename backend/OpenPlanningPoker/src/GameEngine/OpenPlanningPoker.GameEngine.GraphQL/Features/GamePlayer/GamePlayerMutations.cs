namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer
{
    [MutationType]
    public class GamePlayerMutations
    {
        /// <summary>
        /// Join Game
        /// </summary>
        public async Task<bool> JoinGameAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Service] ICurrentUserProvider currentUserProvider,
            [Required] Guid gameId,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new JoinGameCommand(gameId, currentUserProvider.CustomerId), cancellationToken);
            return true;
        }

        /// <summary>
        /// Leave a Game
        /// </summary>
        public async Task<bool> LeaveGameAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Service] ICurrentUserProvider currentUserProvider,
            [Required] Guid gameId,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new LeaveGameCommand(gameId, currentUserProvider.CustomerId), cancellationToken);
            return true;
        }
    }
}
