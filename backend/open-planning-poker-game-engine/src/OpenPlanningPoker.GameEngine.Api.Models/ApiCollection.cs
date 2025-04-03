namespace OpenPlanningPoker.GameEngine.Api.Models;

public sealed record ApiCollection<T>(ICollection<T> Items, int TotalCount);