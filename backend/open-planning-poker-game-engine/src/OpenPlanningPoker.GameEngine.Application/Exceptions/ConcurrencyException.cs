namespace OpenPlanningPoker.GameEngine.Application.Exceptions;

public sealed class ConcurrencyException(string message, Exception innerException) : Exception(message, innerException);