import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { ApiCollectionOfGamePlayer } from '../../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-players',
  templateUrl: './players.component.html',
  styleUrl: './players.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayersComponent {

  @Input() players: ApiCollectionOfGamePlayer | undefined;
}
