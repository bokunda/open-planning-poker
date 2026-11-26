namespace OpenPlanningPoker.GameEngine.GraphQL.Features;

/// <summary>
/// Base Game Engine query.
/// </summary>
[QueryType]
public static class Query
{
    /// <summary>
    /// Dummy health-check query.
    /// </summary>
    /// <returns></returns>
    // Both subgraphs have it, so Fusion needs it to be @shareable.
    [HotChocolate.Types.Composite.Shareable]
    public static string Ping() => "Pong.";
}
