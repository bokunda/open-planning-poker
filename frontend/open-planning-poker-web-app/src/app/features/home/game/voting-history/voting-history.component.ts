import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Ticket } from '../../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-voting-history',
  templateUrl: './voting-history.component.html',
  styleUrl: './voting-history.component.scss',
  standalone: true,
  imports: [CommonModule, MatTooltipModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VotingHistoryComponent {

  @Input() tickets: Ticket[] = [];
  @Input() votesRevealed = false;
  @Input() currentTicketId: string | undefined;
  @Input() isHost = false;

  @Output() onReVoteTicket = new EventEmitter<string>();

  isRevealed(ticket: Ticket): boolean {
    return !!ticket.votesRevealed || (ticket.id === this.currentTicketId && this.votesRevealed);
  }

  trackByTicketId(index: number, ticket: { id: string }): string {
    return ticket.id;
  }

  trackByVoteId(index: number, vote: { id: string }): string {
    return vote.id;
  }
}
