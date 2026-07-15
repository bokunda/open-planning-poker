import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { Ticket } from '../../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-voting-history',
  templateUrl: './voting-history.component.html',
  styleUrl: './voting-history.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VotingHistoryComponent {

  @Input() tickets: Ticket[] = [];
  @Input() votesRevealed = false;
  @Input() currentTicketId: string | undefined;

  trackByTicketId(index: number, ticket: { id: string }): string {
    return ticket.id;
  }

  trackByVoteId(index: number, vote: { id: string }): string {
    return vote.id;
  }
}
