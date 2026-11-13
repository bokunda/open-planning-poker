import { Component, DoCheck, Input } from '@angular/core';

import { ApiCollectionOfGamePlayer, Vote } from '../../../../graphql/graphql-gateway.service';
import { PokerCardComponent } from '../../../../shared/poker-card/poker-card.component';
import { computeVoteStats, formatNumber } from './vote-stats';
import { TranslocoPipe } from '@jsverse/transloco';

interface Seat {
  id: string;
  name: string;
  isMe: boolean;
  vote: Vote | undefined;
  /** position on the table edge, in % of the table box */
  x: number;
  y: number;
  /** order around the table, starting at the current user (for the staggered reveal) */
  order: number;
  outlier: boolean;
}

/**
 * The table: every player has a seat on the edge with their card; the center shows how many
 * voted and, after the reveal, the average. The current user always sits at the bottom.
 */
@Component({
  selector: 'app-poker-table',
  standalone: true,
  imports: [TranslocoPipe, PokerCardComponent],
  templateUrl: './poker-table.component.html',
  styleUrl: './poker-table.component.scss',
})
export class PokerTableComponent implements DoCheck {
  @Input() players: ApiCollectionOfGamePlayer | undefined;
  @Input() votes: Vote[] = [];
  @Input() votesRevealed = false;
  @Input() currentUserId = '';
  @Input() deck: string[] = [];
  /** Hides the current user's card while it is flying from the hand to the seat. */
  @Input() hideMyCard = false;
  /** Seconds left on the host's timer, or null when no timer runs. */
  @Input() timerRemaining: number | null = null;
  @Input() timerTotal = 60;

  seats: Seat[] = [];
  votedCount = 0;
  average = '–';

  get total(): number { return this.seats.length; }
  get progress(): number { return this.total ? this.votedCount / this.total : 0; }
  get crowded(): boolean { return this.seats.length > 8; }
  get timerActive(): boolean { return this.timerRemaining !== null; }
  get timerFraction(): number { return this.timerActive && this.timerTotal ? Math.max(0, this.timerRemaining! / this.timerTotal) : 0; }
  get timerLabel(): string {
    const s = Math.max(0, this.timerRemaining ?? 0);
    return `${Math.floor(s / 60)}:${(s % 60).toString().padStart(2, '0')}`;
  }

  private signature = '';

  // The parent updates the players and votes arrays in place, so compare their content.
  ngDoCheck(): void {
    const items = this.players?.items ?? [];
    const signature = [
      this.currentUserId,
      this.votesRevealed,
      this.deck.join(','),
      items.map(p => `${p.id}:${p.name}`).join('|'),
      this.votes.map(v => `${v.playerId}:${v.value}`).join('|'),
    ].join('#');
    if (signature === this.signature) return;
    this.signature = signature;
    this.buildSeats(items);
  }

  private buildSeats(items: { id: string; name?: string | null }[]): void {
    const meIndex = items.findIndex(p => p.id === this.currentUserId);
    // Rotate the list so the current user is first, i.e. at the bottom of the table.
    const ordered = meIndex > 0 ? [...items.slice(meIndex), ...items.slice(0, meIndex)] : [...items];
    const n = ordered.length;

    const stats = this.votesRevealed ? computeVoteStats(this.votes, this.deck) : null;
    const outliers = new Set([...(stats?.lowPlayerIds ?? []), ...(stats?.highPlayerIds ?? [])]);

    this.seats = ordered.map((p, i) => {
      // Evenly around an ellipse, starting at the bottom (90°) and going clockwise.
      const angle = (Math.PI / 2) + (i * 2 * Math.PI) / Math.max(n, 1);
      return {
        id: p.id,
        name: p.name ?? 'Player',
        isMe: p.id === this.currentUserId,
        vote: this.votes.find(v => v.playerId === p.id),
        x: 50 + 50 * Math.cos(angle),
        y: 50 + 50 * Math.sin(angle),
        order: i,
        outlier: outliers.has(p.id),
      };
    });

    this.votedCount = this.seats.filter(s => !!s.vote).length;
    this.average = stats ? formatNumber(stats.average) : '–';
  }

  trackBySeat(_: number, seat: Seat): string {
    return seat.id;
  }
}
