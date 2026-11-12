import { Vote } from '../../../../graphql/graphql-gateway.service';

export type ConsensusStatus = 'consensus' | 'close' | 'spread' | 'none';

export interface VoteCount {
  value: string;
  count: number;
  /** count relative to the most frequent value (0..1), for bar widths */
  share: number;
}

export interface VoteStats {
  counts: VoteCount[];
  numericCount: number;
  average: number | null;
  median: number | null;
  min: number | null;
  max: number | null;
  status: ConsensusStatus;
  /** players whose vote is the lowest / highest numeric value (only for a wide spread) */
  lowPlayerIds: string[];
  highPlayerIds: string[];
}

/** Numeric value of a card, or null for cards like "?" or "☕". */
export function cardNumber(value: string | null | undefined): number | null {
  if (value == null) return null;
  const v = value.trim();
  if (v === '½') return 0.5;
  return /^\d+(\.\d+)?$/.test(v) ? Number(v) : null;
}

export function formatNumber(n: number | null): string {
  if (n === null) return '–';
  return (Math.round(n * 10) / 10).toString();
}

/**
 * Summary of revealed votes. `deck` (the game's cards, in order) decides the bar order and
 * whether two values are neighbours ("close to consensus").
 */
export function computeVoteStats(votes: Vote[], deck: string[]): VoteStats {
  const cast = votes.filter(v => !!v.value);
  const order = [...deck, ...cast.map(v => v.value).filter(v => !deck.includes(v))];

  const raw = order
    .map(value => ({ value, count: cast.filter(v => v.value === value).length }))
    .filter(c => c.count > 0);
  const top = Math.max(1, ...raw.map(c => c.count));
  const counts = raw.map(c => ({ ...c, share: c.count / top }));

  const numbers = cast.map(v => cardNumber(v.value)).filter((n): n is number => n !== null).sort((a, b) => a - b);
  const numericCount = numbers.length;
  const average = numericCount ? numbers.reduce((a, b) => a + b, 0) / numericCount : null;
  const mid = Math.floor(numericCount / 2);
  const median = numericCount ? (numericCount % 2 ? numbers[mid] : (numbers[mid - 1] + numbers[mid]) / 2) : null;
  const min = numericCount ? numbers[0] : null;
  const max = numericCount ? numbers[numericCount - 1] : null;

  let status: ConsensusStatus = 'none';
  if (numericCount > 0) {
    if (min === max) {
      status = 'consensus';
    } else {
      const numericDeck = deck.map(cardNumber).filter((n): n is number => n !== null).sort((a, b) => a - b);
      const gap = numericDeck.indexOf(max!) - numericDeck.indexOf(min!);
      status = gap === 1 ? 'close' : 'spread';
    }
  }

  const spread = status === 'spread';
  return {
    counts,
    numericCount,
    average,
    median,
    min,
    max,
    status,
    lowPlayerIds: spread ? cast.filter(v => cardNumber(v.value) === min).map(v => v.playerId) : [],
    highPlayerIds: spread ? cast.filter(v => cardNumber(v.value) === max).map(v => v.playerId) : [],
  };
}

/**
 * The card the team most likely agrees on: the most common card; on a tie the one closest to
 * the median, then the higher one (same rule as the PDF report). Null without votes.
 */
export function suggestedEstimate(votes: Pick<Vote, 'value'>[]): string | null {
  const cast = votes.map(v => v.value).filter((v): v is string => !!v);
  if (!cast.length) return null;
  const numbers = cast.map(cardNumber).filter((n): n is number => n !== null).sort((a, b) => a - b);
  const mid = Math.floor(numbers.length / 2);
  const median = numbers.length ? (numbers.length % 2 ? numbers[mid] : (numbers[mid - 1] + numbers[mid]) / 2) : null;
  const counts = new Map<string, number>();
  for (const v of cast) counts.set(v, (counts.get(v) ?? 0) + 1);
  return [...counts.entries()]
    .sort(([a, ca], [b, cb]) => {
      if (cb !== ca) return cb - ca;
      const na = cardNumber(a);
      const nb = cardNumber(b);
      const da = median !== null && na !== null ? Math.abs(na - median) : Number.MAX_VALUE;
      const db = median !== null && nb !== null ? Math.abs(nb - median) : Number.MAX_VALUE;
      if (da !== db) return da - db;
      return (nb ?? -Infinity) - (na ?? -Infinity);
    })[0][0];
}
