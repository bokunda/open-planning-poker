import { factColumn, nextPhase, RetroCard, stacksOf } from './retro.graphql';

const card = (id: string, column: number, groupId: string | null = null): RetroCard => ({
  id, column, text: id, mine: false, authorId: null, groupId, groupTitle: null, myVotes: 0, votes: null,
});

describe('retro helpers', () => {
  it('builds stacks of a column with the merged cards under their head', () => {
    const cards = [card('a', 0), card('b', 0, 'a'), card('c', 1), card('d', 0)];
    const stacks = stacksOf(cards, 0);
    expect(stacks.map(s => s.head.id)).toEqual(['a', 'd']);
    expect(stacks[0].members.map(m => m.id)).toEqual(['b']);
  });

  it('shows a card whose head is not visible on its own', () => {
    expect(stacksOf([card('b', 0, 'gone')], 0).map(s => s.head.id)).toEqual(['b']);
  });

  it('moves through the steps and stops at the end', () => {
    expect(nextPhase('WRITING')).toBe('GROUPING');
    expect(nextPhase('VOTING')).toBe('DISCUSSING');
    expect(nextPhase('DISCUSSING')).toBeNull();
  });

  it('puts sprint facts in the column for things to improve', () => {
    expect(factColumn('went-well')).toBe(1);
    expect(factColumn('4ls')).toBe(2);
  });
});
