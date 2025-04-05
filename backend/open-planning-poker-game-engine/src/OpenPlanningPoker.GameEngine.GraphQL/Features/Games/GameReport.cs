namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

public sealed class GameReport
{
    public Guid GameId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
}
