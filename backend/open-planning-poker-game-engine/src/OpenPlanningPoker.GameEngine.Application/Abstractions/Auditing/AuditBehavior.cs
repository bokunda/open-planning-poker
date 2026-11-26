using System.Collections.Concurrent;
using System.Reflection;

namespace OpenPlanningPoker.GameEngine.Application.Abstractions.Auditing;

/// <summary>
/// Writes the activity log for commands that implement <see cref="IAuditedRequest"/>: the entry is resolved before
/// the handler runs (so a deleted ticket keeps its name) and written only when the handler succeeds. A result of
/// <c>Result&lt;T, ApplicationError&gt;</c> succeeds when <c>IsSuccess</c> is true; any other non-null result when the
/// handler did not throw. A handler that changed nothing calls <see cref="AuditContext.MarkUnchanged"/> and no entry
/// is written.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(IAuditLog auditLog, AuditContext context) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, FieldInfo?> SuccessFields = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IAuditedRequest audited || audited.ToAuditEntry() is not { } entry)
        {
            return await next(cancellationToken);
        }

        var resolved = await auditLog.ResolveAsync(entry, cancellationToken);
        context.Reset();
        var response = await next(cancellationToken);
        var unchanged = context.Unchanged;
        context.Reset();

        if (resolved is not null && !unchanged && IsSuccess(response))
        {
            await auditLog.WriteAsync(resolved, cancellationToken);
        }

        return response;
    }

    public static bool IsSuccess(TResponse response)
    {
        if (response is null)
        {
            return false;
        }

        var field = SuccessFields.GetOrAdd(response.GetType(), static type =>
            type.GetField("IsSuccess", BindingFlags.Public | BindingFlags.Instance) is { FieldType: var fieldType } found && fieldType == typeof(bool)
                ? found
                : null);
        return field is null || (bool)field.GetValue(response)!;
    }
}
