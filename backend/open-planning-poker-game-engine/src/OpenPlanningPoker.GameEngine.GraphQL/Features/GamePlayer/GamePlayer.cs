namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer;

public class GamePlayer
{
    public Guid Id { get; set; }

    /// <summary>Observers watch the table and do not vote.</summary>
    public Domain.GamePlayer.PlayerRole Role { get; set; }

    /// <summary>Co-hosts can run the game next to the host.</summary>
    public bool IsCoHost { get; set; }

    public async Task<string?> GetNameAsync([Service] IUserService userSerivce, CancellationToken cancellationToken = default) => 
        (await userSerivce.GetAsync(Id.ToString(), cancellationToken))?.UserName;
}
