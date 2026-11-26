using OpenPlanningPoker.GameEngine.Domain.Audits;

namespace OpenPlanningPoker.GameEngine.Application.Features.Audits;

/// <summary>
/// The activity log of a game, team, organization or of yourself, newest first (GitHub #20).
/// <paramref name="Before"/> is the time of the oldest entry already shown.
/// </summary>
public sealed record GetAuditLogQuery(AuditScope Scope, Guid ScopeId, DateTimeOffset? Before = null, int Take = GetAuditLog.DefaultTake)
    : IRequest<Result<AuditLogPage, ApplicationError>>;

public sealed record AuditLogPage(IReadOnlyList<Audit> Entries, bool HasMore);

public static class GetAuditLog
{
    public const int DefaultTake = 30;
    public const int MaxTake = 100;

    public sealed class Validator : AbstractValidator<GetAuditLogQuery>
    {
        public Validator()
        {
            RuleFor(x => x.ScopeId).NotEmpty();
            RuleFor(x => x.Scope).IsInEnum();
        }
    }

    /// <summary>Checks who may read the log: host and co-hosts of a game, the team owner, organization admins, yourself.</summary>
    public sealed class Handler(IGameAccessService access, IAuditRepository audits)
        : IRequestHandler<GetAuditLogQuery, Result<AuditLogPage, ApplicationError>>
    {
        public async Task<Result<AuditLogPage, ApplicationError>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
        {
            if (await CheckAccessAsync(request, cancellationToken) is { } error)
            {
                return error;
            }

            var take = Math.Clamp(request.Take, 1, MaxTake);
            var rows = await audits.GetAsync(request.Scope, request.ScopeId, request.Before, take + 1, cancellationToken);
            return new AuditLogPage([.. rows.Take(take)], rows.Count > take);
        }

        private async Task<ApplicationError?> CheckAccessAsync(GetAuditLogQuery request, CancellationToken cancellationToken) =>
            request.Scope switch
            {
                AuditScope.Game => await access.EnsureHostAsync(request.ScopeId, cancellationToken),
                AuditScope.Team => await access.EnsureTeamOwnerAsync(request.ScopeId, cancellationToken),
                AuditScope.Organization => await access.EnsureOrganizationAdminAsync(request.ScopeId, cancellationToken),
                AuditScope.User => access.EnsureAuthenticated()
                    ?? (request.ScopeId == access.CurrentUserId ? null : GameAccessErrors.Forbidden),
                _ => GameAccessErrors.Forbidden
            };
    }
}
