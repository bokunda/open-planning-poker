﻿namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Settings;

[QueryType]
public class SettingsQueries
{
    /// <summary>
    /// Gets Game Settings.
    /// </summary>
    public async Task<FieldResult<Settings, ApplicationError>> GetSettingsAsync(
        [Service] ISender sender,
        [Service] IMapper mapper,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetGameSettingsQuery(gameId), cancellationToken);
        return result.IsSuccess
            ? mapper.Map<Settings>(result.Value)
            : result.Error!;
    }
}
