import { Component, inject, OnInit } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { ApiCollectionOfGamePlayer, BaseUserProfile, Game, GamePlayer, Mutation, MutationCreateGameArgs, MutationCreateTicketArgs, MutationJoinGameArgs, Query, Settings, Ticket } from '../../../graphql/graphql-gateway.service';
import { GET_GAME } from './gql/getGame.graphql';
import { CREATE_GAME } from './gql/createGame.graphql';
import { MatDialog } from '@angular/material/dialog';
import { CreateGameDialogComponent } from './create/dialog/create-game-dialog.component';
import { JoinGameDialogComponent } from './join/dialog/join-game-dialog.component';
import { ActivatedRoute, Router } from '@angular/router';
import { JOIN_GAME } from './gql/joinGame.graphql';
import { GET_GAME_PLAYERS } from './gql/getPlayers.graphql';
import { ON_PLAYER_JOINED } from './gql/onPlayerJoined.graphql';
import { CreateTicketDialogComponent } from './ticket/create-ticket/dialog/create-ticket-dialog.component';
import { CREATE_TICKET } from './gql/createTicket.graphql';
import { GET_TICKET } from './gql/getTicket.graphql';
import { ON_TICKET_CREATED } from './gql/onTicketCreated.graphql';

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrl: './game.component.scss',
  standalone: false
})
export class GameComponent implements OnInit {

  game: Game | undefined;
  ticket: Ticket | undefined;
  players: ApiCollectionOfGamePlayer | undefined;

  readonly dialog = inject(MatDialog);
  readonly router = inject(Router);
  readonly route = inject(ActivatedRoute);

  constructor(private apollo: Apollo) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const gameId = params.get('id');
      const ticketId = params.get('ticketId');
      if (gameId) {
        this.getGame(gameId);
        this.subscribeToPlayerJoined(gameId);
        this.subscribeToTicketCreated(gameId);

        if (ticketId) {
          this.getTicket(ticketId);
        }
      }
    });
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

  handleCreateTicket() {
    this.openCreateTicketDialog();
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

  private subscribeToPlayerJoined(gameId: string): void {
    this.apollo.subscribe<{ onPlayerJoined: BaseUserProfile }>({
      query: ON_PLAYER_JOINED,
      variables: { gameId },
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

  private createTicket(gameId: string, name: string, description: string) {
    this.apollo.mutate<Mutation, MutationCreateTicketArgs>({
      mutation: CREATE_TICKET,
      variables: {
        input: {
          gameId,
          name,
          description
      }}
    }).subscribe({
      next: ({ data }) => {
        if (data?.createTicket?.ticket) {
          this.ticket = data?.createTicket?.ticket;
          this.router.navigate([`/game/${gameId}/ticket/${this.ticket.id}`]);
        }
      },
      error: (error) => {
        console.error('[GameComponent] Error creating a ticket', error);
      },
      complete: () => {
        console.log('[GameComponent] Ticket Creation completed');
      }
    });
  }

  private getTicket(id: string): void {
    this.apollo.watchQuery<{ ticket: Ticket }>({
      query: GET_TICKET,
      variables: { id }
    })
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        if (data) {
          this.ticket = data?.ticket;
        }
      },
      error: (error) => {
        console.error('[GameComponent] Error getting ticket', error);
      },
      complete: () => {
        console.log('[GameComponent] Get ticket request completed');
      }
    });
  }

  private openCreateTicketDialog() {
    const dialogRef = this.dialog.open(CreateTicketDialogComponent, {
      width: '400px',
      maxWidth: '700px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: Ticket | undefined) => {
      if (!result) { return; }
      this.createTicket(this.game?.id, result.name, result.description);
    });
  }

  private subscribeToTicketCreated(gameId: string): void {
    this.apollo.subscribe<{ onTicketCreated: Ticket }>({
      query: ON_TICKET_CREATED,
      variables: { gameId },
    }).subscribe((result) => {
      if (!result) {
        return;
      }
      this.ticket = result.data?.onTicketCreated as Ticket;
    });
  }

}
