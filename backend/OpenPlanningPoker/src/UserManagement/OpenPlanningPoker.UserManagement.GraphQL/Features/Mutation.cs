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
    public static string Ping() => "Pong.";
}
