import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoPipe } from '@jsverse/transloco';
import { RoomTicket } from '../room-types';
import { SortingBoard } from '../gql/sorting.graphql';

/** One ticket on the board. Estimated tickets sit in their card's column and cannot be moved. */
export interface BoardCard {
  ticket: RoomTicket;
  contested: boolean;
  /** Has a final estimate (voted or applied): shown, but locked. */
  done: boolean;
}

export interface BoardColumn {
  /** Card value; null for the unsorted column. */
  value: string | null;
  id: string;
  cards: BoardCard[];
}

const UNSORTED_ID = 'sort-unsorted';

/**
 * Sorting board for long backlogs (OPP-142): one column per card. Players move tickets in silence; tickets the team
 * keeps moving are marked "Discuss" (OPP-143). The host turns the columns into final estimates (OPP-144).
 */
@Component({
  selector: 'app-sorting-board',
  standalone: true,
  imports: [TranslocoPipe, DragDropModule, MatButtonModule, MatIconModule, MatMenuModule, MatTooltipModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sorting-board.component.html',
  styleUrl: './sorting-board.component.scss',
})
export class SortingBoardComponent implements OnChanges {
  @Input() tickets: RoomTicket[] = [];
  @Input() board: SortingBoard | null = null;
  @Input() isHost = false;
  @Input() isObserver = false;

  /** Column null moves the ticket back to unsorted. */
  @Output() move = new EventEmitter<{ ticketId: string; column: string | null }>();
  @Output() clearContested = new EventEmitter<string>();
  @Output() voteOn = new EventEmitter<string>();
  @Output() apply = new EventEmitter<void>();
  @Output() close = new EventEmitter<void>();

  columns: BoardColumn[] = [];
  /** Moves shown at once, before the server confirms them. */
  private readonly pending = new Map<string, string | null>();

  ngOnChanges(): void {
    // A fresh board from the server replaces our guesses.
    this.pending.clear();
    this.build();
  }

  get canMove(): boolean {
    return !this.isObserver;
  }

  get contestedCount(): number {
    return this.columns.reduce((n, c) => n + c.cards.filter(card => card.contested && !card.done).length, 0);
  }

  get sortedCount(): number {
    return this.columns.filter(c => c.value !== null).reduce((n, c) => n + c.cards.filter(card => !card.done).length, 0);
  }

  get openCount(): number {
    return this.columns.reduce((n, c) => n + c.cards.filter(card => !card.done).length, 0);
  }

  get connectedIds(): string[] {
    return this.columns.map(c => c.id);
  }

  columnLabel(column: BoardColumn): string {
    return column.value ?? '';
  }

  drop(event: CdkDragDrop<BoardColumn, BoardColumn, BoardCard>): void {
    if (event.previousContainer === event.container) return;
    this.moveTo(event.item.data, event.container.data.value);
  }

  moveTo(card: BoardCard, column: string | null): void {
    if (!this.canMove || card.done) return;
    const current = this.columnOf(card.ticket.id);
    if (current === column) return;
    this.pending.set(card.ticket.id, column);
    this.build();
    this.move.emit({ ticketId: card.ticket.id, column });
  }

  /** Columns a ticket can move to from its menu (all but its own). */
  targets(card: BoardCard): (string | null)[] {
    const current = this.columnOf(card.ticket.id);
    return [null, ...(this.board?.columns ?? [])].filter(value => value !== current);
  }

  trackColumn(_: number, column: BoardColumn): string {
    return column.id;
  }

  trackCard(_: number, card: BoardCard): string {
    return card.ticket.id;
  }

  private columnOf(ticketId: string): string | null {
    if (this.pending.has(ticketId)) return this.pending.get(ticketId) ?? null;
    return this.board?.tickets.find(t => t.ticketId === ticketId)?.column ?? null;
  }

  private build(): void {
    const values = this.board?.columns ?? [];
    const byValue = new Map<string | null, BoardColumn>();
    byValue.set(null, { value: null, id: UNSORTED_ID, cards: [] });
    values.forEach((value, i) => byValue.set(value, { value, id: `sort-col-${i}`, cards: [] }));
    const contested = new Set((this.board?.tickets ?? []).filter(t => t.contested).map(t => t.ticketId));

    for (const ticket of this.tickets) {
      const done = !!ticket.finalEstimate;
      const value = done ? ticket.finalEstimate! : this.columnOf(ticket.id);
      const column = byValue.get(value) ?? byValue.get(null)!;
      column.cards.push({ ticket, contested: !done && contested.has(ticket.id), done });
    }
    // Open tickets first in each column, so the work left is on top.
    for (const column of byValue.values()) {
      column.cards.sort((a, b) => Number(a.done) - Number(b.done));
    }
    this.columns = [...byValue.values()];
  }
}
