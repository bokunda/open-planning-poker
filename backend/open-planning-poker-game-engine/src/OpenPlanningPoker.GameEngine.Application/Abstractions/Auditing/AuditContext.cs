namespace OpenPlanningPoker.GameEngine.Application.Abstractions.Auditing;

/// <summary>
/// Lets an audited handler tell <see cref="AuditBehavior{TRequest, TResponse}"/> that a successful call changed
/// nothing (e.g. saving the deck the game already has, which the room does right after a game is created), so no
/// entry is written. Scoped: the handler and the behavior of one request share it.
/// </summary>
public sealed class AuditContext
{
    public bool Unchanged { get; private set; }

    /// <summary>Call from the handler when the request left everything as it was.</summary>
    public void MarkUnchanged() => Unchanged = true;

    internal void Reset() => Unchanged = false;
}
