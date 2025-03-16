import { Component, inject, OnInit } from '@angular/core';
import { Apollo } from 'apollo-angular';
import { ApiCollectionOfGamePlayer, ApiCollectionOfTicket, ApiCollectionOfVote, BaseUserProfile, Game, GamePlayer, Mutation, MutationCreateGameArgs, MutationCreateOrUpdateVoteArgs, MutationCreateTicketArgs, MutationJoinGameArgs, Ticket, Vote } from '../../../graphql/graphql-gateway.service';
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
import { ON_VOTE_CREATED_OR_UPDATED } from './gql/onVoteCreatedOrUpdated.graphql';
import { CREATE_OR_UPDATE_VOTE } from './gql/createVote.graphql';
import { GET_VOTES } from './gql/getVotes.graphql';
import { GET_TICKETS } from './gql/getTickets.graphql';

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrl: './game.component.scss',
  standalone: false
})
export class GameComponent implements OnInit {

  game: Game | undefined;
  ticket: Ticket | undefined;
  tickets: Ticket[] = [];
  players: ApiCollectionOfGamePlayer | undefined;
  votes: Vote[] = [];

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
          this.subscribeToVoteActions(ticketId);
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

  handleVoteAction(value: string) {
    if (!this.ticket?.id) { return; }
    this.createOrUpdateVote(this.ticket!.id, value);
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
          this.getTickets(this.game.id);
        }
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

  private createTicket(gameId: string, name: string, description: string): void {
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
          this.votes = [];
          this.subscribeToPlayerJoined(gameId);
          this.getTickets(gameId);
          this.router.navigate([`/game/${gameId}/ticket/${this.ticket.id}`]);
        }
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
          this.getVotes(this.ticket.id);
          this.getTickets(this.ticket.gameId);
        }
      }
    });
  }

  private openCreateTicketDialog(): void {
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
      this.votes = [];
      this.router.navigate([`/game/${gameId}/ticket/${this.ticket.id}`]);
    });
  }

  private getVotes(ticketId: string) {
    this.apollo.watchQuery<{ votes: ApiCollectionOfVote }>({
      query: GET_VOTES,
      variables: { ticketId }
    })
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        if (data) {
          if (data?.votes?.items) {
            data.votes.items.forEach(vote => {
              this.votes.push(vote);
            });
          }
        }
      }
    });
  }

  private createOrUpdateVote(ticketId: string, value: string): void {
    this.apollo.mutate<Mutation, MutationCreateOrUpdateVoteArgs>({
      mutation: CREATE_OR_UPDATE_VOTE,
      variables: {
        input: {
          ticketId,
          value
      }}
    }).subscribe({
      next: ({ data }) => {
        if (data?.createOrUpdateVote?.vote) {
          const voteIndex = this.votes.findIndex(x => x && x.id === data!.createOrUpdateVote!.vote!.id);
          if (voteIndex !== -1) {
            this.votes[voteIndex].value = data.createOrUpdateVote.vote.value;
          } else {
            this.votes.push(data.createOrUpdateVote.vote);
          }
        }
      }
    });
  }

  private subscribeToVoteActions(ticketId: string): void {
    this.apollo.subscribe<{ onVoteCreatedOrUpdated: any }>({
      query: ON_VOTE_CREATED_OR_UPDATED,
      variables: { ticketId },
    }).subscribe((result) => {
      if (!result) {
        return;
      }
      const voteIndex = this.votes.findIndex(x => x && x.id === result!.data!.onVoteCreatedOrUpdated!.id);

      if (voteIndex !== -1) {
        this.votes = this.votes.map((vote, index) =>
          index === voteIndex ? { ...vote, value: result!.data!.onVoteCreatedOrUpdated!.value } : vote
        );
      } else {
        this.votes.push(result!.data!.onVoteCreatedOrUpdated);
      }
    });
  }

  private getTickets(gameId: string): void {
    this.apollo.watchQuery<{ tickets: ApiCollectionOfTicket }>({
      query: GET_TICKETS,
      variables: { gameId }
    })
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        if (data) {
          this.tickets = data?.tickets?.items ?? [];
        }
      }
    });
  }

}
