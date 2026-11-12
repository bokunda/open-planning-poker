import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiCollectionOfGamePlayer, Vote } from '../../../../graphql/graphql-gateway.service';
import { PlayerAction, RoomPlayer } from '../room-types';
import { PokerCardComponent } from '../../../../shared/poker-card/poker-card.component';

@Component({
  selector: 'app-players',
  templateUrl: './players.component.html',
  styleUrl: './players.component.scss',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatMenuModule, MatTooltipModule, PokerCardComponent],
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
  /** The game's host (runs it; cannot be removed). */
  @Input() hostId: string | undefined;
  /** The current user is the host (not only a co-host): can name co-hosts and hand the game over. */
  @Input() isOwner = false;

  @Output() onPlayerAction = new EventEmitter<PlayerAction>();

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

  asRoomPlayer(player: unknown): RoomPlayer {
    return player as RoomPlayer;
  }

  isObserver(player: RoomPlayer): boolean {
    return player.role === 'OBSERVER';
  }

  /** Who gets the "⋮" menu: yourself (watch / play), and everyone but the host for hosts and co-hosts. */
  canManage(player: RoomPlayer): boolean {
    return player.id === this.currentUserId || (this.isHost && player.id !== this.hostId);
  }

  roleLabel(player: RoomPlayer): string {
    if (player.id === this.hostId) return 'Host';
    if (player.isCoHost) return 'Co-host';
    return '';
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

  /** Observers do not vote, so they do not count towards "x/y voted". */
  get totalPlayers(): number {
    return (this.players?.items as RoomPlayer[] | undefined)?.filter(p => p.role !== 'OBSERVER').length ?? 0;
  }

  get consensusLabel(): string {
    if (!this.totalPlayers) return '';
    return `${this.votedCount}/${this.totalPlayers} voted`;
  }

  trackByPlayerId(index: number, player: { id: string }): string {
    return player.id;
  }
}
