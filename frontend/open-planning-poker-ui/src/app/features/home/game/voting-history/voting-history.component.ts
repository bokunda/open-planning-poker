import { ChangeDetectionStrategy, Component, Input, signal } from '@angular/core';
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
}
