namespace OpenPlanningPoker.Shared.Errors;

public sealed record ApplicationError(string Code, string? Message = null);
