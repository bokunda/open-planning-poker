import { looksTooBig } from './ai-assist.component';

describe('looksTooBig', () => {
  const deck = ['0', '0.5', '1', '2', '3', '5', '8', '13', '❓', '⏸️'];

  it('is true when someone played the highest card', () => {
    expect(looksTooBig(['5', '13', '8'], deck)).toBeTrue();
  });

  it('is true when a third of the votes are "?"', () => {
    expect(looksTooBig(['❓', '3', '5'], deck)).toBeTrue();
  });

  it('is false for an ordinary spread and without votes', () => {
    expect(looksTooBig(['3', '5', '5'], deck)).toBeFalse();
    expect(looksTooBig([], deck)).toBeFalse();
  });
});
