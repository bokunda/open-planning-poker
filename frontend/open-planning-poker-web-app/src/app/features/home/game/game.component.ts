import { Component, inject, Input, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import { REVEAL_VOTES } from './gql/revealVotes.graphql';
import { ON_VOTES_REVEALED } from './gql/onVotesRevealed.graphql';
import { GET_TICKETS } from './gql/getTickets.graphql';
import { GENERATE_GAME_REPORT } from './gql/generateGameReport.graphql';
import { UPDATE_SETTINGS } from './gql/updateSettings.graphql';
import { MutationRevealVotesArgs, MutationUpdateSettingsArgs, RevealVotesPayload, UpdateSettingsPayload, VotesRevealed } from '../../../graphql/graphql-gateway.service';
import { CreateGameResult } from './create/create-game.component';
import { map } from 'rxjs';

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
  votesRevealed = false;

  @Input() currentUserId: string = '';

  playersCollapsed = false;
  chatCollapsed = false;
  private gameSubsInitialized = false;

  get isLoading(): boolean {
    // Only show loading when navigating to a specific game route (has gameId in URL)
    // and game data hasn't loaded yet. On the landing page (!gameId), don't show loader.
    const hasGameId = !!this.route.snapshot.paramMap.get('id');
    return hasGameId && !this.game;
  }

  get isHost(): boolean {
    if (!this.game?.id) return false;
    const storedHost = localStorage.getItem('host_' + this.game.id);
    // If we have currentUserId, check exact match. Fallback: any stored host is valid.
    if (this.currentUserId) return storedHost === this.currentUserId;
    return !!storedHost;
  }

  readonly dialog = inject(MatDialog);
  readonly router = inject(Router);
  readonly route = inject(ActivatedRoute);
  readonly destroyRef = inject(DestroyRef);

  constructor(private apollo: Apollo) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const gameId = params.get('id');
      const ticketId = params.get('ticketId');
      if (gameId) {
        // Game-level subs: only once
        if (!this.gameSubsInitialized) {
          this.getGame(gameId);
          this.subscribeToPlayerJoined(gameId);
          this.subscribeToTicketCreated(gameId);
          this.gameSubsInitialized = true;
        }

        if (ticketId) {
          // Reset ticket state before loading new ticket
          this.votes = [];
          this.votesRevealed = false;
          this.ticket = undefined;
          this.getTicket(ticketId);
          this.subscribeToVoteActions(ticketId);
          this.subscribeToVotesRevealed(ticketId);
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
    this.gameSubsInitialized = false;
    this.router.navigate([`/game`]);
  }

  handleGameReport() {
    this.generateAndDownloadReport(this.game?.id!);
  }

  handleCreateTicket() {
    this.openCreateTicketDialog();
  }

  handleVoteAction(value: string) {
    if (!this.ticket?.id) { return; }
    this.createOrUpdateVote(this.ticket!.id, value);
  }

  handleRevealVotes() {
    if (!this.ticket?.id) return;
    this.apollo.mutate<RevealVotesPayload, MutationRevealVotesArgs>({
      mutation: REVEAL_VOTES,
      variables: { input: { ticketId: this.ticket.id } }
    }).subscribe({
      next: () => this.votesRevealed = true,
      error: (err) => console.error('Reveal votes error:', err)
    });
  }

  handleVoteAgain() {
    this.votesRevealed = false;
    this.votes = [];
  }

  handleReVoteTicket(ticketId: string) {
    this.router.navigate([`/game/${this.game?.id}/ticket/${ticketId}`]);
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

  private createGame(name: string, description: string, deckSetup: string): void {
    this.apollo.mutate<Mutation, MutationCreateGameArgs>({
      mutation: CREATE_GAME,
      variables: {input: {name, description}}
    }).subscribe({
      next: ({ data }) => {
        if (data?.createGame?.game) {
          this.game = data?.createGame?.game;
          localStorage.setItem('host_' + this.game.id, this.currentUserId);

          // Apply deck setup via updateSettings (gateway.fgp doesn't have deckSetup in CreateGameInput yet)
          const settingsId = (data.createGame.game as any).settingsDetails?.id;
          if (settingsId && deckSetup) {
            this.applyDeckSetup(this.game.id, settingsId, deckSetup);
          }

          this.joinGame(this.game.id);
          this.router.navigate([`/game/${this.game.id}`]);
        }
      }
    });
  }

  private applyDeckSetup(gameId: string, settingsId: string, deckSetup: string): void {
    this.apollo.mutate<UpdateSettingsPayload, MutationUpdateSettingsArgs>({
      mutation: UPDATE_SETTINGS,
      variables: { input: { id: settingsId, gameId, deckSetup } }
    }).subscribe();
  }

  private getGamePlayers(gameId: string): void {
    this.apollo.watchQuery<{ gamePlayers: ApiCollectionOfGamePlayer }>({
      query: GET_GAME_PLAYERS,
      variables: { gameId }
    })
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        if (data.gamePlayers?.items) {
          // Merge with existing to avoid duplicates from subscription race condition
          const existing = this.players?.items ?? [];
          const existingIds = new Set(existing.map(p => p.id));
          const newItems = data.gamePlayers.items.filter(p => !existingIds.has(p.id));
          this.players = {
            ...data.gamePlayers,
            items: [...existing, ...newItems],
            totalCount: existing.length + newItems.length
          };
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
      width: '440px',
      maxWidth: '95vw',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: CreateGameResult | undefined) => {
      if (!result) { return; }
      this.createGame(result.name, result.description, result.deckSetup);
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
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (!result) {
          return;
        }

        const newPlayer: BaseUserProfile = result!.data!.onPlayerJoined;
        const item = { id: newPlayer.id, name: newPlayer.userName } as GamePlayer;

        if (this.players?.items) {
          const existingPlayerIndex = this.players.items.findIndex(player => player.id === item.id);
          if (existingPlayerIndex !== -1) {
            if (this.players.items[existingPlayerIndex].name !== item.name) {
              this.players.items[existingPlayerIndex].name = item.name;
            }
          } else {
            this.players = {
              ...this.players,
              items: [...this.players.items, item],
              totalCount: (this.players.totalCount ?? 0) + 1
            };
          }
        } else {
          this.players = { items: [item], totalCount: 1 } as ApiCollectionOfGamePlayer;
        }
      },
      error: (err) => {
        console.error('Subscription error (onPlayerJoined):', err);
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
          this.votesRevealed = false;
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
          this.votesRevealed = false;
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
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (!result) {
          return;
        }
        this.ticket = result.data?.onTicketCreated as Ticket;
        this.votes = [];
        this.votesRevealed = false;
        this.getTickets(gameId);
        this.router.navigate([`/game/${gameId}/ticket/${this.ticket.id}`]);
      },
      error: (err) => {
        console.error('Subscription error (onTicketCreated):', err);
      }
    });
  }

  private getVotes(ticketId: string) {
    this.votes = [];
    this.apollo.watchQuery<{ votes: ApiCollectionOfVote }>({
      query: GET_VOTES,
      variables: { ticketId }
    })
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        if (data?.votes?.items) {
          this.votes = data.votes.items;
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
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (!result) {
          return;
        }
        const voteIndex = this.votes.findIndex(x => x && x.id === result!.data!.onVoteCreatedOrUpdated!.id);

        this.getTickets(this.game?.id!);
        if (voteIndex !== -1) {
          this.votes = this.votes.map((vote, index) =>
            index === voteIndex ? { ...vote, value: result!.data!.onVoteCreatedOrUpdated!.value } : vote
          );
        } else {
          this.votes.push(result!.data!.onVoteCreatedOrUpdated);
        }
      },
      error: (err) => {
        console.error('Subscription error (onVoteCreatedOrUpdated):', err);
      }
    });
  }

  private subscribeToVotesRevealed(ticketId: string): void {
    this.apollo.subscribe<{ onVotesRevealed: VotesRevealed }>({
      query: ON_VOTES_REVEALED,
      variables: { ticketId },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.votesRevealed = true;
      },
      error: (err) => console.error('Subscription error (onVotesRevealed):', err)
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

          if (!this.ticket || !this.tickets) {
            return;
          }

          this.ticket!.averageVotingValue = this.tickets.find(x => x.id == this.ticket!.id)?.averageVotingValue;
        }
      }
    });
  }

  generateAndDownloadReport(gameId: string): void {
    this.apollo.mutate({
      mutation: GENERATE_GAME_REPORT,
      variables: {
        input: {
          gameId: gameId
        }
      }
    }).pipe(
      map((result: any) => result.data?.generateGameReport?.gameReport)
    ).subscribe((report) => {
      if (!report) {
        return;
      }

      const byteArray = new Uint8Array(report.data);
      const blob = new Blob([byteArray], { type: 'application/pdf' });

      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = report.fileName;
      a.click();
    });
  }
}
