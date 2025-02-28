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
    public static string Ping() => "Pong.";
}
