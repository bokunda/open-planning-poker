namespace OpenPlanningPoker.GameEngine.GraphQL.Features
{
    public sealed record ApiCollection<T>(ICollection<T> Items, int TotalCount);
}
