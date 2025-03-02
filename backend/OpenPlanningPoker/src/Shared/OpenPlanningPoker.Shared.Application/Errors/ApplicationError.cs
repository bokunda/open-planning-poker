namespace OpenPlanningPoker.Shared.Application.Errors;

public sealed record ApplicationError(string Code, string? Message = null);
