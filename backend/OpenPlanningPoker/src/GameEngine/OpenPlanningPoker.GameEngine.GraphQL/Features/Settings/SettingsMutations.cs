namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Settings
{
    [MutationType]
    public class SettingsMutations
    {
        /// <summary>
        /// Creates Game Settings.
        /// </summary>
        public async Task<Settings> CreateSettingsAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid gameId,
            [Required] int votingTime,
            [Required] bool isBreakAllowed,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new CreateGameSettingsCommand(gameId, votingTime, isBreakAllowed), cancellationToken);
            return mapper.Map<Settings>(result);
        }

        /// <summary>
        /// Updates Game Settings.
        /// </summary>
        public async Task<Settings> UpdateSettingsAsync(
            [Service] ISender sender,
            [Service] IMapper mapper,
            [Required] Guid id,
            [Required] Guid gameId,
            [Required] int votingTime,
            [Required] bool isBreakAllowed,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new UpdateGameSettingsCommand(id, gameId, votingTime, isBreakAllowed), cancellationToken);
            return mapper.Map<Settings>(result);
        }
    }
}
