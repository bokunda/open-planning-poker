import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ApiCollectionOfGamePlayer, Vote } from '../../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-players',
  templateUrl: './players.component.html',
  styleUrl: './players.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Default
})
export class PlayersComponent implements OnInit {

  ngOnInit(): void { }

  @Input() players: ApiCollectionOfGamePlayer | undefined;
  @Input() votes: Vote[] = [];

  @Output() onCreateNewTicket = new EventEmitter<void>();
  @Output() onVoteAgain = new EventEmitter<void>();
  @Output() onRevealCards = new EventEmitter<void>();

  get topPlayers() {
    return this.players!.items.slice(0, Math.ceil(this.players!.items.length / 4));
  }

  get rightPlayers() {
    return this.players!.items.slice(
      Math.ceil(this.players!.items.length / 4),
      Math.ceil(this.players!.items.length / 2)
    );
  }

  get bottomPlayers() {
    return this.players!.items.slice(
      Math.ceil(this.players!.items.length / 2),
      Math.ceil((3 * this.players!.items.length) / 4)
    );
  }

  get leftPlayers() {
    return this.players!.items.slice(Math.ceil((3 * this.players!.items.length) / 4));
  }

  getVoteData(playerId: string): Vote | undefined {
    return this.votes.find(x => x.playerId === playerId);
  }
}
