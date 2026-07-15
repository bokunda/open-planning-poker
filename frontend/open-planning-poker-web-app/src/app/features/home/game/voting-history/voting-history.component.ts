import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Ticket } from '../../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-voting-history',
  templateUrl: './voting-history.component.html',
  styleUrl: './voting-history.component.scss',
  standalone: true,
  imports: [CommonModule, MatExpansionModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VotingHistoryComponent {

  @Input() tickets: Ticket[] = [];
  @Input() votesRevealed = false;
  @Input() currentTicketId: string | undefined;
  @Input() isHost = false;

  @Output() onReVoteTicket = new EventEmitter<string>();

  trackByTicketId(index: number, ticket: { id: string }): string {
    return ticket.id;
  }

  trackByVoteId(index: number, vote: { id: string }): string {
    return vote.id;
  }
}
