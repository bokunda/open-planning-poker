// Turns activity log entries (GitHub #20) into translation keys and parameters. Kept free of Angular so it is easy to test.

export type AuditScope = 'GAME' | 'TEAM' | 'ORGANIZATION' | 'USER';
export type AuditType = 'CREATE' | 'UPDATE' | 'DELETE';

export interface ActivityEntry {
  id: string;
  action: string;
  type: AuditType;
  actorId: string;
  actorName: string | null;
  targetId: string | null;
  targetName: string | null;
  ticketId: string | null;
  detail: string | null;
  value: string | null;
  createdOn: string;
}

export const EMPTY_ID = '00000000-0000-0000-0000-000000000000';

/** Every action code the backend writes (AuditActions.cs); others show a generic sentence. */
export const KNOWN_ACTIONS = [
  'ticket.created', 'ticket.updated', 'ticket.deleted', 'tickets.imported', 'tickets.reordered', 'ticket.estimated',
  'votes.revealed', 'votes.hidden', 'votes.reset',
  'player.role', 'player.coHostGranted', 'player.coHostRevoked', 'player.removed', 'host.transferred',
  'settings.deck', 'settings.options', 'settings.facilitation', 'recap.shared', 'recap.unshared', 'sorting.applied',
  'team.updated', 'team.memberJoined', 'team.memberRemoved', 'team.anchorPinned', 'team.anchorUnpinned',
  'team.apiTokenCreated', 'team.apiTokenRevoked', 'team.webhookCreated', 'team.webhookUpdated',
  'team.webhookSecretRotated', 'team.webhookDeleted',
  'org.renamed', 'org.inviteRotated', 'org.memberJoined', 'org.memberRemoved', 'org.memberRole', 'org.teamAdded', 'org.teamRemoved',
  'integration.connected', 'integration.disconnected',
] as const;

/** Actions where "removed" reads "left" when people did it to themselves. */
const SELF_ACTIONS = new Set(['team.memberRemoved', 'org.memberRemoved', 'player.role']);

/** `votes.revealed` → `activity.actions.votesRevealed`; unknown codes → `activity.actions.unknown`. */
export function actionKey(entry: Pick<ActivityEntry, 'action' | 'actorId' | 'targetId'>): string {
  if (!(KNOWN_ACTIONS as readonly string[]).includes(entry.action)) return 'activity.actions.unknown';
  const camel = entry.action.replace(/\.(\w)/g, (_, c: string) => c.toUpperCase());
  const self = SELF_ACTIONS.has(entry.action) && !!entry.targetId && entry.targetId === entry.actorId;
  return `activity.actions.${camel}${self ? 'Self' : ''}`;
}

/** Material icon for the kind of change. */
export function actionIcon(type: AuditType): string {
  return type === 'CREATE' ? 'add_circle' : type === 'DELETE' ? 'remove_circle' : 'edit';
}

const PROVIDERS: Record<string, string> = { jira: 'Jira', github: 'GitHub', linear: 'Linear', azuredevops: 'Azure DevOps' };

/** The provider name for integration entries, as the product calls itself. */
export function providerName(value: string | null): string {
  if (!value) return '';
  return PROVIDERS[value.toLowerCase()] ?? value;
}

/** The oldest entry's time, to ask for the next page. */
export function nextCursor(entries: readonly Pick<ActivityEntry, 'createdOn'>[]): string | null {
  return entries.length ? entries[entries.length - 1].createdOn : null;
}

/** Adds a new page without showing an entry twice (the cursor is a time, so two pages can touch). */
export function appendPage(current: readonly ActivityEntry[], page: readonly ActivityEntry[]): ActivityEntry[] {
  const seen = new Set(current.map((entry) => entry.id));
  return [...current, ...page.filter((entry) => !seen.has(entry.id))];
}
