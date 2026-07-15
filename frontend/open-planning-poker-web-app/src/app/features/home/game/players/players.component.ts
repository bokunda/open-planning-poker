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
  @Input() votesRevealed = false;
  @Input() isHost = false;
  @Input() collapsed = false;
  @Input() currentUserId: string = '';

  @Output() onCreateNewTicket = new EventEmitter<void>();
  @Output() onVoteAgain = new EventEmitter<void>();
  @Output() onRevealCards = new EventEmitter<void>();
  @Output() collapsedChange = new EventEmitter<boolean>();

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

  getVoteDisplay(playerId: string): string {
    if (!this.votesRevealed) {
      return this.getVoteData(playerId) ? '?' : '';
    }
    return this.getVoteData(playerId)?.value ?? '';
  }

  get hasAnyVotes(): boolean {
    return this.votes.length > 0;
  }

  get votedCount(): number {
    return this.votes.length;
  }

  get totalPlayers(): number {
    return this.players?.totalCount ?? 0;
  }

  get consensusLabel(): string {
    if (!this.totalPlayers) return '';
    return `${this.votedCount}/${this.totalPlayers} voted`;
  }

  trackByPlayerId(index: number, player: { id: string }): string {
    return player.id;
  }
}
