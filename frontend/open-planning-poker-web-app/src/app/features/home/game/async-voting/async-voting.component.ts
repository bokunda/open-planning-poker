import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Ticket } from '../../../../graphql/graphql-gateway.service';
import { MarkdownPipe } from '../../../../shared/markdown/markdown.pipe';
import { TranslocoPipe } from '@jsverse/transloco';
import { OppDatePipe } from '../../../../i18n/opp-date.pipe';

/**
 * Async estimation: before the meeting every player goes through the tickets on their own.
 * Votes stay hidden (the server only returns your own values) until the host reveals each
 * ticket in the meeting, so nobody anchors the others.
 */
@Component({
  selector: 'app-async-voting',
  standalone: true,
  imports: [TranslocoPipe, OppDatePipe, MatButtonModule, MatIconModule, MarkdownPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './async-voting.component.html',
  styleUrl: './async-voting.component.scss',
})
export class AsyncVotingComponent {
  @Input() tickets: Ticket[] = [];
  @Input() deck: string[] = [];
  @Input() currentUserId = '';
  @Input() isHost = false;
  @Input() isObserver = false;
  @Input() endsAt: string | null | undefined;

  @Output() vote = new EventEmitter<{ ticketId: string; value: string }>();
  @Output() startMeeting = new EventEmitter<void>();
  @Output() createTicket = new EventEmitter<void>();
  @Output() importTickets = new EventEmitter<void>();

  /** Votes picked here before the server confirms them, so the card lights up at once. */
  private readonly pending = new Map<string, string>();

  myVote(ticket: Ticket): string | null {
    return this.pending.get(ticket.id)
      ?? ticket.votes?.items?.find(v => v.playerId === this.currentUserId && !!v.value)?.value
      ?? null;
  }

  voteCount(ticket: Ticket): number {
    return ticket.votes?.items?.length ?? 0;
  }

  /** Tickets you voted on, among those still to be revealed. */
  get votedCount(): number {
    return this.openTickets.filter(t => this.myVote(t) !== null).length;
  }

  get openTickets(): Ticket[] {
    return this.tickets.filter(t => !t.votesRevealed);
  }

  pick(ticket: Ticket, value: string): void {
    if (this.isObserver || ticket.votesRevealed) return;
    this.pending.set(ticket.id, value);
    this.vote.emit({ ticketId: ticket.id, value });
  }

  trackById(index: number, item: { id: string }): string {
    return item.id;
  }
}
