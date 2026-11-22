import { comparisonCsv } from './org-helpers';

describe('comparisonCsv', () => {
  it('writes one row per team and keeps formulas out of spreadsheets', () => {
    const csv = comparisonCsv([
      { teamId: '1', name: 'Payments, EU', sprints: 3, firstRoundConsensusShare: 0.567, reVotesPerTicket: 0.3, averageVelocity: 21, velocity: [13, 21, 29] },
      { teamId: '2', name: '=HYPERLINK("x")', sprints: 0, firstRoundConsensusShare: null, reVotesPerTicket: null, averageVelocity: null, velocity: [] },
    ]);
    const lines = csv.trim().split('\n');
    expect(lines.length).toBe(3);
    expect(lines[1]).toBe('"Payments, EU",3,0.57,0.3,21,13 21 29');
    expect(lines[2].startsWith(`"'=HYPERLINK`)).toBeTrue();
  });
});
