namespace OpenPlanningPoker.Shared.GraphGL.Errors;

public sealed record GraphQlError(string Code, string? Message = null);
