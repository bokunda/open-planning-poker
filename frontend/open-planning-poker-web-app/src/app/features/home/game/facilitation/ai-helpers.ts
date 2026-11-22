/** "?" and break cards say nothing about size. */
const UNSURE = /[?❓]/u;

/**
 * After the reveal, the team found the ticket too big: the highest card of the deck, or at least a third of the votes "?"
 * (OPP-151).
 */
export function looksTooBig(votes: string[], deck: string[]): boolean {
  if (!votes.length) return false;
  const sized = deck.filter(c => !UNSURE.test(c) && !/[☕⏸]/u.test(c));
  const highest = sized[sized.length - 1];
  const unsure = votes.filter(v => UNSURE.test(v)).length;
  return (!!highest && votes.includes(highest)) || unsure * 3 >= votes.length;
}
