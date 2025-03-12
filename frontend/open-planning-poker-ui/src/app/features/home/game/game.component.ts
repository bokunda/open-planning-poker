import { Component, inject, OnInit } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { ApiCollectionOfGamePlayer, BaseUserProfile, Game, GamePlayer, Mutation, MutationCreateGameArgs, MutationJoinGameArgs, Query, Settings } from '../../../graphql/graphql-gateway.service';
import { GET_GAME } from './gql/getGame.graphql';
import { CREATE_GAME } from './gql/createGame.graphql';
import { MatDialog } from '@angular/material/dialog';
import { CreateGameDialogComponent } from './create/dialog/create-game-dialog.component';
import { JoinGameDialogComponent } from './join/dialog/join-game-dialog.component';
import { ActivatedRoute, Router } from '@angular/router';
import { JOIN_GAME } from './gql/joinGame.graphql';
import { GET_GAME_PLAYERS } from './gql/getPlayers.graphql';
import { ON_PLAYER_JOINED } from './gql/onPlayerJoined.graphql';

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrl: './game.component.scss',
  standalone: false
})
export class GameComponent implements OnInit {

  game: Game | undefined;
  players: ApiCollectionOfGamePlayer | undefined;

  readonly dialog = inject(MatDialog);
  readonly router = inject(Router);
  readonly route = inject(ActivatedRoute);

  constructor(private apollo: Apollo) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const gameId = params.get('id');
      if (gameId) {
        this.getGame(gameId);
      }
    });

    this.subscribeToPlayerJoined();
  }

  handleCreateGame() {
    this.openCreateGameDialog();
  }

  handleJoinGame() {
    this.openJoinGameDialog();
  }

  handleLeaveGame() {
    this.game = undefined;
    this.router.navigate([`/game`]);
  }

  private getGame(id: string): void {
    this.apollo.watchQuery<{ game: Game }>({
      query: GET_GAME,
      variables: { id }
    })
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        if (data) {
          this.game = data?.game;
          this.joinGame(this.game.id);
        }
      },
      error: (error) => {
        console.error('[GameComponent] Error getting game', error);
      },
      complete: () => {
        console.log('[GameComponent] Get game request completed');
      }
    });
  }

  private createGame(name: string, description: string): void {
    this.apollo.mutate<Mutation, MutationCreateGameArgs>({
      mutation: CREATE_GAME,
      variables: {input: {name, description}}
    }).subscribe({
      next: ({ data }) => {
        if (data?.createGame?.game) {
          this.game = data?.createGame?.game;
          this.joinGame(this.game.id);
          this.router.navigate([`/game/${this.game.id}`]);
        }
      },
      error: (error) => {
        console.error('[GameComponent] Error creating a game', error);
      },
      complete: () => {
        console.log('[GameComponent] Game Creation completed');
      }
    });
  }

  private getGamePlayers(gameId: string): void {
    this.apollo.watchQuery<{ gamePlayers: ApiCollectionOfGamePlayer }>({
      query: GET_GAME_PLAYERS,
      variables: { gameId }
    })
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        if (data.gamePlayers) {
          this.players = data.gamePlayers;
        }
      }
    });
  }

  private joinGame(gameId: string): void {
    this.apollo.mutate<Mutation, MutationJoinGameArgs>({
      mutation: JOIN_GAME,
      variables: {input: {gameId}}
    }).subscribe({
      next: ({ data }) => {
        this.getGamePlayers(gameId);
      }
    });
  }

  private openCreateGameDialog() {
    const dialogRef = this.dialog.open(CreateGameDialogComponent, {
      width: '400px',
      maxWidth: '400px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: Game | undefined) => {
      if (!result) { return; }
      this.createGame(result.name, result.description);
    });
  }

  private openJoinGameDialog() {
    const dialogRef = this.dialog.open(JoinGameDialogComponent, {
      width: '400px',
      maxWidth: '400px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: Game | undefined) => {
      if (!result) { return; }
      this.getGame(result.id);
    });
  }

  private subscribeToPlayerJoined(): void {
    this.apollo.subscribe<{ onPlayerJoined: BaseUserProfile }>({
      query: ON_PLAYER_JOINED,
      variables: {},
    }).subscribe((result) => {
      if (!result) {
        return;
      }

      const newPlayer: BaseUserProfile = result!.data!.onPlayerJoined;
      const item = { id: newPlayer.id, name: newPlayer.userName } as GamePlayer;

      if (this.players?.items) {
        const existingPlayerIndex = this.players.items.findIndex(player => player.id === item.id);
        if (existingPlayerIndex !== -1) {
          // Update the username if it is different
          if (this.players.items[existingPlayerIndex].name !== item.name) {
            this.players.items[existingPlayerIndex].name = item.name;
          }
        } else {
          // Add the new player
          this.players = {
            ...this.players,
            items: [...this.players.items, item]
          };
        }
      } else {
        this.players = { items: [item] } as ApiCollectionOfGamePlayer;
      }
    });
  }

}
