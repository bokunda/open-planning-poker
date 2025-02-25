namespace OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer
{
    public class GamePlayer
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
    }
}
