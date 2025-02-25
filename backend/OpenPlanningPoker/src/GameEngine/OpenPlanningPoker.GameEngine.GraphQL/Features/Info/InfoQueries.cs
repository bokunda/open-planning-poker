namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Info
{
    [QueryType]
    public class InfoQueries
    {
        /// <summary>
        /// Returns an info about the Game Engine service.
        /// </summary>
        public async Task<Info> GetInfoAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetInfoQuery(), cancellationToken);
            return mapper.Map<Info>(result);
        }
    }
}
