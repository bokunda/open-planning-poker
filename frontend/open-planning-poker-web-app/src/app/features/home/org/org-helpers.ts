/** One team's numbers in the organization comparison (OPP-157). */
export interface TeamComparison {
  teamId: string;
  name: string;
  sprints: number;
  firstRoundConsensusShare: number | null;
  reVotesPerTicket: number | null;
  averageVelocity: number | null;
  velocity: number[];
}

const cell = (value: string | number | null): string => {
  const text = value === null ? '' : String(value);
  // Quote when needed; a leading =, +, - or @ would run as a formula in a spreadsheet.
  const safe = /^[=+\-@]/.test(text) ? `'${text}` : text;
  return /[",\n;]/.test(safe) ? `"${safe.replace(/"/g, '""')}"` : safe;
};

/** The comparison as CSV for a spreadsheet: one row per team, velocity oldest first. */
export function comparisonCsv(rows: TeamComparison[]): string {
  const header = ['Team', 'Sessions', 'First-vote agreement', 'Re-votes per ticket', 'Average velocity', 'Velocity (oldest first)'];
  const lines = rows.map(r => [
    cell(r.name), cell(r.sprints),
    cell(r.firstRoundConsensusShare === null ? null : Math.round(r.firstRoundConsensusShare * 100) / 100),
    cell(r.reVotesPerTicket), cell(r.averageVelocity), cell(r.velocity.join(' ')),
  ].join(','));
  return [header.join(','), ...lines].join('\n') + '\n';
}
