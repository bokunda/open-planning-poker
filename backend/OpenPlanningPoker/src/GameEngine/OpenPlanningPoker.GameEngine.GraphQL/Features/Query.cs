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
    public static string Ping() => "Pong.";
}
