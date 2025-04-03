namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

public class Vote
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Value { get; set; } = string.Empty;

    public async Task<string> GetPlayerName(
        [Service] IUserService userService,
        CancellationToken cancellationToken = default) =>
        (await userService.GetAsync(PlayerId.ToString(), cancellationToken))
            ?.UserName ?? string.Empty;
}
