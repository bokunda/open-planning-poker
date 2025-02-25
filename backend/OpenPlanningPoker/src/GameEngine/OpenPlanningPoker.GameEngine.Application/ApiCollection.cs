namespace OpenPlanningPoker.GameEngine.Application;

public sealed record ApiCollection<T>(ICollection<T> Items, int TotalCount);