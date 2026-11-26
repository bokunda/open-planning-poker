namespace OpenPlanningPoker.UserManagement.GraphQL.Features;

/// <summary>
/// Base User Management query.
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
