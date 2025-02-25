namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games
{
    [QueryType]
    public class GameQueries
    {
        /// <summary>
        /// Returns Game details.
        /// </summary>
        public async Task<Game> GetGameAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid id,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetGameQuery(id), cancellationToken);
            return mapper.Map<Game>(result);
        }
    }
}
