namespace OpenPlanningPoker.UserManagement.GraphQL.Features;

/// <summary>
/// Base User Management mutation.
/// </summary>
[MutationType]
public static class Mutation
{
    /// <summary>
    /// Dummy health-check mutation.
    /// </summary>
    /// <returns></returns>
    // Both subgraphs have it, so Fusion needs it to be @shareable.
    [HotChocolate.Types.Composite.Shareable]
    public static string Ping() => "Pong.";
}
