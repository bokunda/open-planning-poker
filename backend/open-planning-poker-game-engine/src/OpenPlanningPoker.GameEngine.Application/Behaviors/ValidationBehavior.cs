using System.Collections.Concurrent;
using System.Reflection;
using ValidationException = OpenPlanningPoker.GameEngine.Application.Exceptions.ValidationException;
using ValidationError = OpenPlanningPoker.GameEngine.Application.Exceptions.ValidationError;

namespace OpenPlanningPoker.GameEngine.Application.Behaviors;

/// <summary>
/// Runs the FluentValidation validators of a request before its handler.
/// For handlers returning <c>Result&lt;T, ApplicationError&gt;</c> a failure becomes an
/// <see cref="ApplicationError"/> (code "ValidationFailed"), so GraphQL returns it as a typed error.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, MethodInfo?> ErrorConversions = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(result => result.Errors).Where(failure => failure is not null).ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var conversion = ErrorConversions.GetOrAdd(typeof(TResponse), FindErrorConversion);
        if (conversion is null)
        {
            throw new ValidationException(failures.Select(failure => new ValidationError(failure.PropertyName, failure.ErrorMessage)));
        }

        var message = string.Join(" ", failures.Select(failure => failure.ErrorMessage).Distinct());
        return (TResponse)conversion.Invoke(null, [new ApplicationError("ValidationFailed", message)])!;
    }

    // Result<TValue, ApplicationError> has an implicit conversion from ApplicationError.
    private static MethodInfo? FindErrorConversion(Type responseType) =>
        responseType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == "op_Implicit"
                && method.ReturnType == responseType
                && method.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType == typeof(ApplicationError));
}
