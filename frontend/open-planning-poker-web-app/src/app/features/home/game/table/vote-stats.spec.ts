import { Vote } from '../../../../graphql/graphql-gateway.service';
import { cardNumber, computeVoteStats, formatNumber } from './vote-stats';

const deck = ['0', '0.5', '1', '2', '3', '5', '8', '13', '❓', '⏸️'];
const vote = (playerId: string, value: string) => ({ id: playerId, playerId, value } as Vote);

describe('vote-stats', () => {
  it('parses card numbers', () => {
    expect(cardNumber('5')).toBe(5);
    expect(cardNumber('0.5')).toBe(0.5);
    expect(cardNumber('½')).toBe(0.5);
    expect(cardNumber('❓')).toBeNull();
    expect(cardNumber('')).toBeNull();
  });

  it('detects consensus', () => {
    const s = computeVoteStats([vote('a', '5'), vote('b', '5')], deck);
    expect(s.status).toBe('consensus');
    expect(s.average).toBe(5);
  });

  it('treats neighbouring cards as close to consensus', () => {
    expect(computeVoteStats([vote('a', '3'), vote('b', '5')], deck).status).toBe('close');
  });

  it('finds the outliers of a wide spread', () => {
    const s = computeVoteStats([vote('a', '3'), vote('b', '5'), vote('c', '5'), vote('d', '13'), vote('e', '8')], deck);
    expect(s.status).toBe('spread');
    expect(s.lowPlayerIds).toEqual(['a']);
    expect(s.highPlayerIds).toEqual(['d']);
    expect(s.median).toBe(5);
    expect(formatNumber(s.average)).toBe('6.8');
    expect(s.counts.map(c => c.value)).toEqual(['3', '5', '8', '13']);
  });

  it('ignores non-numeric cards in the numbers but counts them', () => {
    const s = computeVoteStats([vote('a', '❓'), vote('b', '⏸️')], deck);
    expect(s.status).toBe('none');
    expect(s.average).toBeNull();
    expect(s.counts.length).toBe(2);
  });
});
