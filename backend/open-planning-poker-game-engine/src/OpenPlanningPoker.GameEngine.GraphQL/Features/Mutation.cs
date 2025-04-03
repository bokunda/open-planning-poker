namespace OpenPlanningPoker.GameEngine.GraphQL.Features;

/// <summary>
/// Base Game Engine mutation.
/// </summary>
[MutationType]
public static class Mutation
{
    /// <summary>
    /// Dummy health-check mutation.
    /// </summary>
    /// <returns></returns>
    public static string Ping() => "Pong.";
}
