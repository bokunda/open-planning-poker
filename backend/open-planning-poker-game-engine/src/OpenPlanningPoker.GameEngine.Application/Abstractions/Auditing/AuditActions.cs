namespace OpenPlanningPoker.GameEngine.Application.Abstractions.Auditing;

/// <summary>
/// The action codes of the activity log. They are stored and sent to the app, which translates them
/// (<c>activity.actions.*</c> in the i18n files), so never rename one; add a new code instead.
/// </summary>
public static class AuditActions
{
    // Game room
    public const string TicketCreated = "ticket.created";
    public const string TicketUpdated = "ticket.updated";
    public const string TicketDeleted = "ticket.deleted";
    public const string TicketsImported = "tickets.imported";
    public const string TicketsReordered = "tickets.reordered";
    public const string TicketEstimated = "ticket.estimated";
    public const string VotesRevealed = "votes.revealed";
    public const string VotesHidden = "votes.hidden";
    public const string VotesReset = "votes.reset";
    public const string PlayerRole = "player.role";
    public const string PlayerCoHostGranted = "player.coHostGranted";
    public const string PlayerCoHostRevoked = "player.coHostRevoked";
    public const string PlayerRemoved = "player.removed";
    public const string HostTransferred = "host.transferred";
    public const string SettingsDeck = "settings.deck";
    public const string SettingsOptions = "settings.options";
    public const string SettingsFacilitation = "settings.facilitation";
    public const string RecapShared = "recap.shared";
    public const string RecapUnshared = "recap.unshared";
    public const string SortingApplied = "sorting.applied";

    // Team room
    public const string TeamUpdated = "team.updated";
    public const string TeamMemberJoined = "team.memberJoined";
    /// <summary>Left the team (the actor is the member) or was removed.</summary>
    public const string TeamMemberRemoved = "team.memberRemoved";
    public const string TeamAnchorPinned = "team.anchorPinned";
    public const string TeamAnchorUnpinned = "team.anchorUnpinned";
    public const string TeamApiTokenCreated = "team.apiTokenCreated";
    public const string TeamApiTokenRevoked = "team.apiTokenRevoked";
    public const string TeamWebhookCreated = "team.webhookCreated";
    public const string TeamWebhookUpdated = "team.webhookUpdated";
    public const string TeamWebhookSecretRotated = "team.webhookSecretRotated";
    public const string TeamWebhookDeleted = "team.webhookDeleted";

    // Organization
    public const string OrganizationRenamed = "org.renamed";
    public const string OrganizationInviteRotated = "org.inviteRotated";
    public const string OrganizationMemberJoined = "org.memberJoined";
    /// <summary>Left the organization (the actor is the member) or was removed.</summary>
    public const string OrganizationMemberRemoved = "org.memberRemoved";
    public const string OrganizationMemberRole = "org.memberRole";
    public const string OrganizationTeamAdded = "org.teamAdded";
    public const string OrganizationTeamRemoved = "org.teamRemoved";

    // A user's own integrations
    public const string IntegrationConnected = "integration.connected";
    public const string IntegrationDisconnected = "integration.disconnected";
}
