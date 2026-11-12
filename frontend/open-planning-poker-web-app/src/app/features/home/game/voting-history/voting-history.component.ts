import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { Ticket } from '../../../../graphql/graphql-gateway.service';
import { RoomTicket } from '../room-types';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-voting-history',
  templateUrl: './voting-history.component.html',
  styleUrl: './voting-history.component.scss',
  standalone: true,
  imports: [TranslocoPipe, CommonModule, DragDropModule, MatTooltipModule, MatButtonModule, MatIconModule, MatMenuModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VotingHistoryComponent {

  @Input() tickets: Ticket[] = [];
  @Input() votesRevealed = false;
  @Input() currentTicketId: string | undefined;
  @Input() isHost = false;

  @Output() onReVoteTicket = new EventEmitter<string>();
  @Output() onEditTicket = new EventEmitter<RoomTicket>();
  @Output() onDeleteTicket = new EventEmitter<RoomTicket>();
  /** New order of all tickets (ids, first first). */
  @Output() onReorder = new EventEmitter<string[]>();

  isRevealed(ticket: Ticket): boolean {
    return !!ticket.votesRevealed || (ticket.id === this.currentTicketId && this.votesRevealed);
  }

  finalEstimate(ticket: Ticket): string | null {
    return (ticket as RoomTicket).finalEstimate ?? null;
  }

  /** The host's final estimate, otherwise the average of the revealed votes. */
  estimate(ticket: Ticket): string | null {
    const final = this.finalEstimate(ticket);
    if (final) return final;
    return this.isRevealed(ticket) && ticket.votes?.items?.length ? String(ticket.averageVotingValue ?? '') : null;
  }

  drop(event: CdkDragDrop<Ticket[]>): void {
    if (event.previousIndex === event.currentIndex) return;
    const ids = this.tickets.map(t => t.id);
    moveItemInArray(ids, event.previousIndex, event.currentIndex);
    this.onReorder.emit(ids);
  }

  /** Keyboard alternative to dragging: move a ticket one place up or down. */
  move(index: number, delta: -1 | 1): void {
    const target = index + delta;
    if (target < 0 || target >= this.tickets.length) return;
    const ids = this.tickets.map(t => t.id);
    moveItemInArray(ids, index, target);
    this.onReorder.emit(ids);
  }

  asRoomTicket(ticket: Ticket): RoomTicket {
    return ticket as RoomTicket;
  }

  trackByTicketId(index: number, ticket: { id: string }): string {
    return ticket.id;
  }

  trackByVoteId(index: number, vote: { id: string }): string {
    return vote.id;
  }
}
