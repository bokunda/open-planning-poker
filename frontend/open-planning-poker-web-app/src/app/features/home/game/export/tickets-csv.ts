import { cardNumber, formatNumber, suggestedEstimate } from '../table/vote-stats';
import { RoomTicket } from '../room-types';

/** Quotes a CSV cell; a leading =, +, - or @ is prefixed so spreadsheets do not run it as a formula. */
function cell(value: string | number | null | undefined): string {
  let text = value == null ? '' : String(value);
  if (/^[=+\-@\t\r]/.test(text)) text = `'${text}`;
  return /[",\n\r;]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

/**
 * The game's tickets as CSV, one row per ticket in list order: final estimate (or the most common
 * card), average and each player's vote. Votes of tickets that were not revealed are left out.
 */
export function ticketsToCsv(tickets: RoomTicket[]): string {
  const header = ['#', 'Ticket', 'Description', 'Estimate', 'Estimate source', 'Average', 'Votes', 'Revealed'];
  const rows = tickets.map((ticket, index) => {
    const revealed = !!ticket.votesRevealed;
    const votes = revealed ? (ticket.votes?.items ?? []).filter(v => !!v.value) : [];
    const suggested = suggestedEstimate(votes);
    const estimate = ticket.finalEstimate ?? suggested;
    const numbers = votes.map(v => cardNumber(v.value)).filter((n): n is number => n !== null);
    const average = numbers.length ? formatNumber(numbers.reduce((a, b) => a + b, 0) / numbers.length) : '';
    return [
      index + 1,
      ticket.name,
      ticket.description,
      estimate ?? '',
      ticket.finalEstimate ? 'Final (host)' : suggested ? 'Most common card' : '',
      average,
      votes.map(v => `${v.playerName}: ${v.value}`).join('; '),
      revealed ? 'Yes' : 'No',
    ].map(cell).join(',');
  });
  return [header.join(','), ...rows].join('\r\n');
}
