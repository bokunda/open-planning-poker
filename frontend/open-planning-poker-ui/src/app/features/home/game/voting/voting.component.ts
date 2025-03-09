import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { ApiCollectionOfGamePlayer } from '../../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-voting',
  templateUrl: './voting.component.html',
  styleUrl: './voting.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VotingComponent {

  @Input() players: ApiCollectionOfGamePlayer | undefined;

}
