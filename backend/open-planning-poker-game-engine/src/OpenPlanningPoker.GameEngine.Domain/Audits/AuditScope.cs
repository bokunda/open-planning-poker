namespace OpenPlanningPoker.GameEngine.Domain.Audits;

/// <summary>What an audit entry belongs to, and so who can read it.</summary>
public enum AuditScope
{
    /// <summary>A game room; the host and co-hosts read it.</summary>
    Game = 1,

    /// <summary>A team room; the team owner reads it.</summary>
    Team,

    /// <summary>An organization; its admins read it.</summary>
    Organization,

    /// <summary>A user's own settings (integrations); only that user reads it.</summary>
    User
}
