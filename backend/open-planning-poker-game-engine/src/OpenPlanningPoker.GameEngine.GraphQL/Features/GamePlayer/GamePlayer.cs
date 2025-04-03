namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer;

public class GamePlayer
{
    public Guid Id { get; set; }

    public async Task<string?> GetNameAsync([Service] IUserService userSerivce, CancellationToken cancellationToken = default) => 
        (await userSerivce.GetAsync(Id.ToString(), cancellationToken))?.UserName;
}
