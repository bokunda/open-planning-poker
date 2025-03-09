import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

@Component({
  selector: 'app-voting-history',
  templateUrl: './voting-history.component.html',
  styleUrl: './voting-history.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VotingHistoryComponent {
  readonly panelOpenState = signal(false);
}
