import { ActivityEntry, actionIcon, actionKey, appendPage, nextCursor, providerName } from './activity-helpers';

const entry = (id: string, createdOn = '2026-11-26T10:00:00Z'): ActivityEntry => ({
  id, action: 'votes.revealed', type: 'UPDATE', actorId: 'a', actorName: 'Ana', targetId: null, targetName: null,
  ticketId: null, detail: null, value: null, createdOn,
});

describe('activity helpers', () => {
  it('turns action codes into translation keys', () => {
    expect(actionKey({ action: 'votes.revealed', actorId: 'a', targetId: null })).toBe('activity.actions.votesRevealed');
    expect(actionKey({ action: 'player.coHostGranted', actorId: 'a', targetId: 'b' })).toBe('activity.actions.playerCoHostGranted');
    expect(actionKey({ action: 'something.new', actorId: 'a', targetId: null })).toBe('activity.actions.unknown');
  });

  it('says "left" when people removed themselves', () => {
    expect(actionKey({ action: 'team.memberRemoved', actorId: 'a', targetId: 'a' })).toBe('activity.actions.teamMemberRemovedSelf');
    expect(actionKey({ action: 'team.memberRemoved', actorId: 'a', targetId: 'b' })).toBe('activity.actions.teamMemberRemoved');
  });

  it('picks icons and provider names', () => {
    expect(actionIcon('CREATE')).toBe('add_circle');
    expect(actionIcon('DELETE')).toBe('remove_circle');
    expect(providerName('github')).toBe('GitHub');
    expect(providerName(null)).toBe('');
  });

  it('pages without duplicates', () => {
    const first = [entry('1'), entry('2', '2026-11-25T10:00:00Z')];
    expect(nextCursor(first)).toBe('2026-11-25T10:00:00Z');
    expect(nextCursor([])).toBeNull();
    expect(appendPage(first, [entry('2'), entry('3')]).map((e) => e.id)).toEqual(['1', '2', '3']);
  });
});
