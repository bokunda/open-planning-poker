import { Component, inject, Input, OnInit, DestroyRef, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Apollo } from 'apollo-angular';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ApiCollectionOfGamePlayer, ApiCollectionOfTicket, ApiCollectionOfVote, BaseUserProfile, Game, GamePlayer, Mutation, MutationCreateGameArgs, MutationCreateOrUpdateVoteArgs, MutationCreateTicketArgs, MutationJoinGameArgs, Ticket, Vote } from '../../../graphql/graphql-gateway.service';
import { GET_GAME } from './gql/getGame.graphql';
import { CREATE_GAME } from './gql/createGame.graphql';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Location } from '@angular/common';
import { JOIN_GAME } from './gql/joinGame.graphql';
import { GET_GAME_PLAYERS } from './gql/getPlayers.graphql';
import { ON_PLAYER_JOINED } from './gql/onPlayerJoined.graphql';
import type { ImportedTicket } from './import/import-tickets-dialog.component';
import { CREATE_TICKET } from './gql/createTicket.graphql';
import { GET_TICKET } from './gql/getTicket.graphql';
import { ON_TICKET_CREATED } from './gql/onTicketCreated.graphql';
import { ON_TICKET_RE_VOTED } from './gql/onTicketReVoted.graphql';
import { ON_VOTE_CREATED_OR_UPDATED } from './gql/onVoteCreatedOrUpdated.graphql';
import { CREATE_OR_UPDATE_VOTE } from './gql/createVote.graphql';
import { GET_VOTES } from './gql/getVotes.graphql';
import { RE_VOTE_TICKET } from './gql/reVoteTicket.graphql';
import { REVEAL_VOTES } from './gql/revealVotes.graphql';
import { ON_VOTES_REVEALED } from './gql/onVotesRevealed.graphql';
import { GET_TICKETS } from './gql/getTickets.graphql';
import { GENERATE_GAME_REPORT } from './gql/generateGameReport.graphql';
import { UPDATE_SETTINGS } from './gql/updateSettings.graphql';
import { MutationReVoteTicketArgs, MutationRevealVotesArgs, MutationUpdateSettingsArgs, ReVoteTicketPayload, RevealVotesPayload, UpdateSettingsPayload, VotesRevealed } from '../../../graphql/graphql-gateway.service';
import type { CreateGameResult } from './create/create-game.component';
import { RoomSkeletonComponent } from '../../../shared/room-skeleton/room-skeleton.component';
import { DemoTableComponent } from './landing/demo-table.component';
import { extractGameId } from './join/extract-game-id';
import { FormsModule } from '@angular/forms';
import { GameDetailsComponent } from './details/game-details.component';
import { VotingComponent } from './voting/voting.component';
import { VotingHistoryComponent } from './voting-history/voting-history.component';
import { PlayersComponent } from './players/players.component';
import { ChatComponent } from './chat/chat.component';
import { AsyncVotingComponent } from './async-voting/async-voting.component';
import { map } from 'rxjs';
import type { DocumentNode } from 'graphql';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OPP_SNACKBAR_DURATION_DEFAULT, OPP_SNACKBAR_MODAL_LABEL_CLOSE } from '../../../shared/constants';
import {
  DELETE_TICKET, ON_GAME_CHANGED, REMOVE_PLAYER, REORDER_TICKETS, SET_CO_HOST, SET_FINAL_ESTIMATE,
  SET_PLAYER_ROLE, TRANSFER_HOST, UPDATE_GAME_OPTIONS, UPDATE_TICKET
} from './gql/room.graphql';
import { GameChanged, isAsyncVotingOpen, PlayerAction, RoomPlayer, RoomSettings, RoomTicket } from './room-types';
import { ticketsToCsv } from './export/tickets-csv';
import type { ConfirmDialogData } from '../../../shared/dialogs/confirm/confirm-dialog.component';
import type { EditTicketResult } from './ticket/edit-ticket/edit-ticket-dialog.component';
import type { RoomOptions } from './details/game-details.component';

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrl: './game.component.scss',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule, RoomSkeletonComponent, DemoTableComponent, GameDetailsComponent, VotingComponent, VotingHistoryComponent, PlayersComponent, ChatComponent, AsyncVotingComponent],
  animations: [
    trigger('fadeSlide', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(12px)' }),
        animate('250ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ opacity: 0, transform: 'translateY(-8px)' }))
      ])
    ]),
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('300ms ease-out', style({ opacity: 1 }))
      ])
    ])
  ]
})
export class GameComponent implements OnInit {

  game: Game | undefined;
  ticket: Ticket | undefined;
  tickets: Ticket[] = [];
  players: ApiCollectionOfGamePlayer | undefined;
  votes: Vote[] = [];
  votesRevealed = false;
  /**
   * Tickets whose votes were revealed in this session. Reveal events and query results can
   * arrive in any order; only a re-vote may turn a revealed ticket back to hidden.
   */
  private readonly revealedTicketIds = new Set<string>();

  @Input() currentUserName: string = '';

  private _currentUserId: string = '';
  @Input()
  set currentUserId(value: string) {
    const previous = this._currentUserId;
    this._currentUserId = value;
    if (!value || previous) {
      return;
    }
    // The server only accepts game actions from a known user, so anything requested
    // before the user identity resolved runs now.
    if (this.pendingCreateGame) {
      const pending = this.pendingCreateGame;
      this.pendingCreateGame = undefined;
      this.createGame(pending.name, pending.description, pending.deckSetup);
    } else if (this.game?.id) {
      this.joinGame(this.game.id);
    }
  }
  get currentUserId(): string {
    return this._currentUserId;
  }

  private pendingCreateGame: CreateGameResult | undefined;
  private joinedGameId: string | undefined;

  playersCollapsed = false;
  chatCollapsed = false;
  private gameSubsInitialized = false;
  private currentSubscribedTicketId: string | undefined;

  /** Set when the game in the URL does not exist (or was deleted). */
  gameNotFound = false;

  /** Landing page: link or code pasted into the inline join field. */
  joinInput = '';
  joinError = '';

  get isLoading(): boolean {
    // Only show loading when navigating to a specific game route (has gameId in URL)
    // and game data hasn't loaded yet. On the landing page (!gameId), don't show loader.
    const hasGameId = !!this.route.snapshot.paramMap.get('id');
    return hasGameId && !this.game && !this.gameNotFound;
  }

  /** The host is decided by the server (the creator until it is handed over); the UI only mirrors it. */
  get isOwner(): boolean {
    return !!this.game?.hostId && !!this.currentUserId && this.game.hostId === this.currentUserId;
  }

  /** Host or co-host: runs the game (tickets, reveal, timer, options). */
  get isHost(): boolean {
    return this.isOwner || !!this.me?.isCoHost;
  }

  get me(): RoomPlayer | undefined {
    return (this.players?.items as RoomPlayer[] | undefined)?.find(p => p.id === this.currentUserId);
  }

  /** Observers see the table but have no cards. */
  get isObserver(): boolean {
    return this.me?.role === 'OBSERVER';
  }

  get settings(): RoomSettings | undefined {
    return this.game?.settingsDetails as unknown as RoomSettings | undefined;
  }

  private deckSource = '';
  private deckCache: string[] = [];

  /** The game's cards (same array until the deck changes, so inputs do not change on every check). */
  get deckCards(): string[] {
    const source = this.settings?.deckSetup ?? '';
    if (source !== this.deckSource) {
      this.deckSource = source;
      this.deckCache = source ? source.split(',') : [];
    }
    return this.deckCache;
  }

  /** Players vote on all tickets on their own until the window ends (async estimation). */
  get asyncVotingOpen(): boolean {
    return isAsyncVotingOpen(this.settings);
  }

  readonly dialog = inject(MatDialog);
  readonly router = inject(Router);
  readonly route = inject(ActivatedRoute);
  readonly location = inject(Location);
  readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly snackBar = inject(MatSnackBar);

  constructor(private apollo: Apollo) {}

  ngOnInit(): void {
    // On a phone the chat starts collapsed so the table and the cards come first.
    if (isPlatformBrowser(this.platformId) && window.matchMedia('(max-width: 767px)').matches) {
      this.chatCollapsed = true;
    }
    this.route.paramMap.subscribe(params => {
      const gameId = params.get('id');
      const ticketId = params.get('ticketId');
      if (gameId) {
        // Game-level subs: only once
        if (!this.gameSubsInitialized) {
          this.getGame(gameId);
          this.subscribeToPlayerJoined(gameId);
          this.subscribeToTicketCreated(gameId);
          this.subscribeToTicketReVoted(gameId);
          this.subscribeToGameChanged(gameId);
          this.gameSubsInitialized = true;
        }

        if (ticketId && ticketId !== this.currentSubscribedTicketId) {
          // Reset ticket state before loading new ticket
          this.votes = [];
          this.votesRevealed = false;
          this.ticket = undefined;
          this.currentSubscribedTicketId = ticketId;
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

  joinFromInput() {
    const id = extractGameId(this.joinInput);
    if (!/^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/i.test(id)) {
      this.joinError = 'That does not look like a game link or code. Copy the link from the host and paste it here.';
      return;
    }
    this.joinError = '';
    this.gameNotFound = false;
    this.getGame(id);
  }

  handleLeaveGame() {
    this.game = undefined;
    this.gameNotFound = false;
    this.joinedGameId = undefined;
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
    if (!this.ticket?.id || this.isObserver) { return; }
    this.createOrUpdateVote(this.ticket!.id, value);
  }

  handleRevealVotes() {
    if (!this.ticket?.id) return;
    this.apollo.mutate<{ revealVotes: RevealVotesPayload }, MutationRevealVotesArgs>({
      mutation: REVEAL_VOTES,
      variables: { input: { ticketId: this.ticket.id } }
    }).subscribe({
      next: ({ data }) => {
        const reveal = data?.revealVotes?.votesRevealed;
        if (reveal) {
          this.applyRevealedVotes(reveal);
        }
      },
      error: (err) => console.error('Reveal votes error:', err)
    });
  }

  /** Votes stay hidden on the server until reveal; the reveal event carries their values. */
  private applyRevealedVotes(reveal: VotesRevealed): void {
    this.revealedTicketIds.add(reveal.ticketId);
    if (this.game?.id) {
      this.getTickets(this.game.id);
    }
    if (reveal.ticketId !== this.ticket?.id) return;
    this.votesRevealed = true;
    if (reveal.votes?.length) {
      this.votes = reveal.votes;
    } else {
      // Payload without votes: load the (now revealed) values from the server.
      this.getVotes(reveal.ticketId);
    }
  }

  private isRevealed(ticket: Ticket | undefined): boolean {
    return !!ticket && (!!ticket.votesRevealed || this.revealedTicketIds.has(ticket.id));
  }

  handleVoteAgain() {
    if (this.ticket?.id) {
      this.handleReVoteTicket(this.ticket.id);
    }
  }

  handleReVoteTicket(ticketId: string) {
    if (!this.game?.id) return;
    // Call the mutation which broadcasts to all players via subscription
    this.apollo.mutate<ReVoteTicketPayload, MutationReVoteTicketArgs>({
      mutation: RE_VOTE_TICKET,
      variables: { input: { gameId: this.game.id, ticketId } }
    }).subscribe({
      error: (err) => console.error('Re-vote ticket error:', err)
    });
  }

  handleImportTickets() {
    this.openImportTicketsDialog();
  }

  private getGame(id: string): void {
    this.apollo.watchQuery<{ game: Game }>({
      query: GET_GAME,
      variables: { id }
    })
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        // The query returns an ApplicationError (e.g. GameNotFound) instead of a Game
        // when the id does not exist or the game was deleted.
        if (data && !data.game?.id) {
          this.gameNotFound = true;
          return;
        }
        if (data) {
          this.gameNotFound = false;
          this.game = data?.game;
          // Without a user yet, the currentUserId setter joins once it is known.
          if (this.currentUserId) {
            this.joinGame(this.game.id);
          }
          this.getTickets(this.game.id);
          // Only move to the game's URL when it is not already open there (joined from the
          // landing page); otherwise the ticket in the URL would be dropped on reload.
          if (this.route.snapshot.paramMap.get('id') !== this.game.id) {
            this.router.navigate([`/game/${this.game.id}`], { replaceUrl: true });
          }
        }
      }
    });
  }

  private createGame(name: string, description: string, deckSetup: string): void {
    // The creator becomes host on the server, so the game can only be created with a known user.
    if (!this.currentUserId) {
      this.pendingCreateGame = { name, description, deckSetup } as CreateGameResult;
      return;
    }

    this.apollo.mutate<Mutation, MutationCreateGameArgs>({
      mutation: CREATE_GAME,
      variables: {input: {name, description}}
    }).subscribe({
      next: ({ data }) => {
        if (data?.createGame?.game) {
          this.game = data?.createGame?.game;

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
    if (this.joinedGameId === gameId) {
      this.getGamePlayers(gameId);
      return;
    }
    this.joinedGameId = gameId;
    this.apollo.mutate<Mutation, MutationJoinGameArgs>({
      mutation: JOIN_GAME,
      variables: {input: {gameId}}
    }).subscribe({
      next: () => {
        this.getGamePlayers(gameId);
      },
      error: (err) => {
        this.joinedGameId = undefined;
        console.error('Join game error:', err);
      }
    });
  }

  private async openCreateGameDialog(): Promise<void> {
    // Loaded on first open, so the dialog is not in the initial bundle.
    const { CreateGameDialogComponent } = await import('./create/dialog/create-game-dialog.component');
    const dialogRef = this.dialog.open(CreateGameDialogComponent, {
      width: '540px',
      maxWidth: '95vw',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: CreateGameResult | undefined) => {
      if (!result) { return; }
      this.createGame(result.name, result.description, result.deckSetup);
    });
  }

  private async openJoinGameDialog(): Promise<void> {
    // Loaded on first open, so the dialog is not in the initial bundle.
    const { JoinGameDialogComponent } = await import('./join/dialog/join-game-dialog.component');
    const dialogRef = this.dialog.open(JoinGameDialogComponent, {
      width: '400px',
      maxWidth: '400px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: { id: string } | undefined) => {
      if (!result?.id) { return; }
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
          if (this.ticket.id !== this.currentSubscribedTicketId) {
            this.currentSubscribedTicketId = this.ticket.id;
            this.subscribeToVoteActions(this.ticket.id);
            this.subscribeToVotesRevealed(this.ticket.id);
          }
          this.location.replaceState(`/game/${gameId}/ticket/${this.ticket.id}`);
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
          this.votesRevealed = this.isRevealed(this.ticket);
          this.getVotes(this.ticket.id);
          this.getTickets(this.ticket.gameId);
        }
      }
    });
  }

  private async openCreateTicketDialog(): Promise<void> {
    // Loaded on first open, so the dialog is not in the initial bundle.
    const { CreateTicketDialogComponent } = await import('./ticket/create-ticket/dialog/create-ticket-dialog.component');
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

  private async openImportTicketsDialog(): Promise<void> {
    // Loaded on first open, so the dialog is not in the initial bundle.
    const { ImportTicketsDialogComponent } = await import('./import/import-tickets-dialog.component');
    const dialogRef = this.dialog.open(ImportTicketsDialogComponent, {
      width: '560px',
      maxWidth: '95vw',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((tickets: ImportedTicket[] | undefined) => {
      if (!tickets?.length) return;
      this.importTicketsSequentially(tickets);
    });
  }

  private importTicketsSequentially(tickets: ImportedTicket[]): void {
    let completed = 0;
    const gameId = this.game?.id;
    if (!gameId) return;
    const created: Ticket[] = [];

    for (const ticket of tickets) {
      this.apollo.mutate<Mutation, MutationCreateTicketArgs>({
        mutation: CREATE_TICKET,
        variables: { input: { gameId, name: ticket.name, description: ticket.description } }
      }).subscribe({
        next: (result) => {
          completed++;
          if (result.data?.createTicket?.ticket) {
            created.push(result.data.createTicket.ticket);
          }
          if (completed === tickets.length) {
            // Preserve input order by matching created tickets to original names
            this.tickets = tickets
              .map(t => created.find(c => c.name === t.name))
              .filter(Boolean) as Ticket[];
            if (this.tickets.length > 0 && !this.ticket) {
              this.ticket = this.tickets[0];
            }
          }
        },
        error: (err) => {
          completed++;
          console.error(`Failed to import ticket "${ticket.name}":`, err);
          if (completed === tickets.length) {
            this.getTickets(gameId);
          }
        }
      });
    }
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
        if (this.ticket?.id && this.ticket.id !== this.currentSubscribedTicketId) {
          this.currentSubscribedTicketId = this.ticket.id;
          this.subscribeToVoteActions(this.ticket.id);
          this.subscribeToVotesRevealed(this.ticket.id);
        }
        this.location.replaceState(`/game/${gameId}/ticket/${this.ticket.id}`);
      },
      error: (err) => {
        console.error('Subscription error (onTicketCreated):', err);
      }
    });
  }

  private subscribeToTicketReVoted(gameId: string): void {
    this.apollo.subscribe<{ onTicketReVoted: Ticket }>({
      query: ON_TICKET_RE_VOTED,
      variables: { gameId },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (!result?.data?.onTicketReVoted) {
          return;
        }
        const ticket = result.data.onTicketReVoted as Ticket;
        this.revealedTicketIds.delete(ticket.id);
        this.ticket = ticket;
        this.votes = [];
        this.votesRevealed = false;
        this.getTickets(gameId);
        if (ticket.id !== this.currentSubscribedTicketId) {
          this.currentSubscribedTicketId = ticket.id;
          this.subscribeToVoteActions(ticket.id);
          this.subscribeToVotesRevealed(ticket.id);
        }
        this.location.replaceState(`/game/${gameId}/ticket/${ticket.id}`);
      },
      error: (err) => {
        console.error('Subscription error (onTicketReVoted):', err);
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
          const vote = data.createOrUpdateVote.vote;
          this.votes = voteIndex !== -1
            ? this.votes.map((v, i) => i === voteIndex ? { ...v, value: vote.value } : v)
            : [...this.votes, vote];
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
        const incoming: Vote = result!.data!.onVoteCreatedOrUpdated;
        const voteIndex = this.votes.findIndex(x => x && x.id === incoming.id);

        this.getTickets(this.game?.id!);
        if (voteIndex !== -1) {
          // Before reveal the broadcast has an empty value; keep the value we already know (our own vote).
          this.votes = this.votes.map((vote, index) =>
            index === voteIndex ? { ...vote, value: incoming.value || vote.value } : vote
          );
        } else {
          this.votes = [...this.votes, incoming];
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
      next: (result) => {
        const reveal = result?.data?.onVotesRevealed;
        if (reveal) {
          this.applyRevealedVotes(reveal);
        }
      },
      error: (err) => console.error('Subscription error (onVotesRevealed):', err)
    });
  }

  private getTickets(gameId: string): void {
    if (!gameId) return;
    this.apollo.query<{ tickets: ApiCollectionOfTicket }>({
      query: GET_TICKETS,
      variables: { gameId }
    })
    .subscribe({
      next: ({ data }) => {
        if (data) {
          this.tickets = data?.tickets?.items ?? [];

          // Keep the active ticket in sync: edited (name, final estimate) or deleted by the host.
          if (this.ticket) {
            const current = this.tickets.find(t => t.id === this.ticket!.id);
            if (current) {
              const { averageVotingValue, votes, ...details } = current as RoomTicket;
              this.ticket = { ...this.ticket, ...details };
            } else if (this.tickets.length || this.ticket.id) {
              this.ticket = undefined;
              this.votes = [];
              this.votesRevealed = false;
              this.currentSubscribedTicketId = undefined;
              if (!this.tickets.length) {
                this.location.replaceState(`/game/${gameId}`);
              }
            }
          }

          // Auto-select a ticket for players who join with no ticket selected: the one in the
          // URL if there is one (its own query may still be loading), otherwise the first.
          if (!this.ticket && this.tickets.length > 0) {
            const urlTicketId = this.route.snapshot.paramMap.get('ticketId');
            const firstTicket = this.tickets.find(t => t.id === urlTicketId) ?? this.tickets[0];
            this.ticket = firstTicket;
            this.votesRevealed = this.isRevealed(firstTicket);
            if (firstTicket?.id && firstTicket.id !== this.currentSubscribedTicketId) {
              this.currentSubscribedTicketId = firstTicket.id;
              this.getVotes(firstTicket.id);
              this.subscribeToVoteActions(firstTicket.id);
              this.subscribeToVotesRevealed(firstTicket.id);
            }
          }

          if (!this.ticket || !this.tickets) {
            return;
          }

          this.ticket!.averageVotingValue = this.tickets.find(x => x.id == this.ticket!.id)?.averageVotingValue;
        }
      }
    });
  }

  // --- Room management (2.2): players, tickets, options ---

  /** One event for "something changed": reload the part it names with our own (checked) queries. */
  private subscribeToGameChanged(gameId: string): void {
    this.apollo.subscribe<{ onGameChanged: GameChanged }>({
      query: ON_GAME_CHANGED,
      variables: { gameId },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        const change = result?.data?.onGameChanged;
        if (!change) return;
        switch (change.kind) {
          case 'GAME':
            this.refreshGame(gameId);
            break;
          case 'PLAYERS':
            this.refreshPlayers(gameId);
            break;
          case 'TICKETS':
            this.getTickets(gameId);
            break;
          case 'PLAYER_REMOVED':
            if (change.playerId === this.currentUserId) {
              this.notify('The host removed you from this game. You can join again with the link.');
              this.handleLeaveGame();
            }
            break;
        }
      },
      error: (err) => console.error('Subscription error (onGameChanged):', err)
    });
  }

  private refreshGame(gameId: string): void {
    this.apollo.query<{ game: Game }>({ query: GET_GAME, variables: { id: gameId } }).subscribe({
      next: ({ data }) => {
        if (data?.game?.id && data.game.id === this.game?.id) {
          this.game = data.game;
        }
      }
    });
  }

  /** Replaces the list (roles, co-hosts, removed players), unlike the merge used while joining. */
  private refreshPlayers(gameId: string): void {
    this.apollo.query<{ gamePlayers: ApiCollectionOfGamePlayer }>({ query: GET_GAME_PLAYERS, variables: { gameId } }).subscribe({
      next: ({ data }) => {
        if (data?.gamePlayers?.items) {
          this.players = { ...data.gamePlayers };
        }
      }
    });
  }

  handlePlayerAction(action: PlayerAction): void {
    const gameId = this.game?.id;
    if (!gameId) return;
    const name = (this.players?.items ?? []).find(p => p.id === action.playerId)?.name ?? 'this player';

    switch (action.kind) {
      case 'role':
        this.runMutation(SET_PLAYER_ROLE, 'setPlayerRole', {
          gameId, role: action.role, playerId: action.playerId === this.currentUserId ? null : action.playerId
        }, () => this.refreshPlayers(gameId));
        break;
      case 'coHost':
        this.runMutation(SET_CO_HOST, 'setCoHost', { gameId, playerId: action.playerId, isCoHost: action.isCoHost },
          () => this.refreshPlayers(gameId));
        break;
      case 'transferHost':
        this.confirm({
          title: `Make ${name} the host?`,
          message: `${name} will run the game. You stay in the game as a player.`,
          confirmLabel: 'Make host',
        }, () => this.runMutation(TRANSFER_HOST, 'transferHost', { gameId, playerId: action.playerId }, () => {
          this.refreshGame(gameId);
          this.refreshPlayers(gameId);
        }));
        break;
      case 'remove':
        this.confirm({
          title: `Remove ${name}?`,
          message: `${name} leaves the table. Their votes stay on the tickets, and they can join again with the link.`,
          confirmLabel: 'Remove',
          destructive: true,
        }, () => this.runMutation(REMOVE_PLAYER, 'removePlayer', { gameId, playerId: action.playerId },
          () => this.refreshPlayers(gameId)));
        break;
    }
  }

  async handleEditTicket(ticket: RoomTicket): Promise<void> {
    const { EditTicketDialogComponent } = await import('./ticket/edit-ticket/edit-ticket-dialog.component');
    this.dialog.open(EditTicketDialogComponent, {
      width: '560px',
      maxWidth: '95vw',
      data: { name: ticket.name, description: ticket.description },
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    }).afterClosed().subscribe((result: EditTicketResult | undefined) => {
      if (!result) return;
      this.runMutation(UPDATE_TICKET, 'updateTicket', { id: ticket.id, name: result.name, description: result.description },
        () => this.getTickets(ticket.gameId));
    });
  }

  handleDeleteTicket(ticket: RoomTicket): void {
    this.confirm({
      title: 'Delete this ticket?',
      message: `"${ticket.name}" and its votes are deleted for everyone. This cannot be undone.`,
      confirmLabel: 'Delete',
      destructive: true,
    }, () => this.runMutation(DELETE_TICKET, 'deleteTicket', { id: ticket.id }, () => this.getTickets(ticket.gameId)));
  }

  handleReorderTickets(ticketIds: string[]): void {
    const gameId = this.game?.id;
    if (!gameId) return;
    // Show the new order at once; the server's order comes back with the change event.
    const byId = new Map(this.tickets.map(t => [t.id, t]));
    this.tickets = ticketIds.map(id => byId.get(id)).filter((t): t is Ticket => !!t);
    this.runMutation(REORDER_TICKETS, 'reorderTickets', { gameId, ticketIds }, undefined, () => this.getTickets(gameId));
  }

  handleSetFinalEstimate(value: string | null): void {
    const ticket = this.ticket;
    if (!ticket?.id) return;
    this.runMutation(SET_FINAL_ESTIMATE, 'setFinalEstimate', { ticketId: ticket.id, value }, () => {
      this.ticket = { ...ticket, finalEstimate: value } as RoomTicket;
      this.getTickets(ticket.gameId);
    });
  }

  handleUpdateOptions(options: RoomOptions): void {
    const gameId = this.game?.id;
    if (!gameId) return;
    this.runMutation(UPDATE_GAME_OPTIONS, 'updateGameOptions', {
      gameId, autoReveal: options.autoReveal, asyncVotingEndsAt: options.asyncVotingEndsAt
    }, () => this.refreshGame(gameId));
  }

  /** Async mode: a vote on any ticket, then the list reloads with our own values. */
  handleAsyncVote(vote: { ticketId: string; value: string }): void {
    this.runMutation(CREATE_OR_UPDATE_VOTE, 'createOrUpdateVote', { ticketId: vote.ticketId, value: vote.value },
      () => { if (this.game?.id) this.getTickets(this.game.id); });
  }

  handleExportCsv(): void {
    if (!this.game) return;
    const csv = ticketsToCsv(this.tickets as RoomTicket[]);
    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `opp_${this.game.name.replace(/[^\p{L}\p{N}]+/gu, '_')}_${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    setTimeout(() => URL.revokeObjectURL(a.href), 1000);
  }

  /** Runs a room mutation; shows the server's error message (e.g. not allowed) as a snackbar. */
  private runMutation(mutation: DocumentNode, field: string, input: Record<string, unknown>, onSuccess?: () => void, onError?: () => void): void {
    this.apollo.mutate<Record<string, { errors?: { code: string; message: string }[] } | undefined>>({
      mutation,
      variables: { input }
    }).subscribe({
      next: ({ data }) => {
        const errors = data?.[field]?.errors;
        if (errors?.length) {
          this.notify(errors[0].message);
          onError?.();
          return;
        }
        onSuccess?.();
      },
      error: () => {
        this.notify('That did not work. Check your connection and try again.');
        onError?.();
      }
    });
  }

  private async confirm(data: ConfirmDialogData, onConfirm: () => void): Promise<void> {
    const { ConfirmDialogComponent } = await import('../../../shared/dialogs/confirm/confirm-dialog.component');
    this.dialog.open(ConfirmDialogComponent, { width: '420px', maxWidth: '95vw', data })
      .afterClosed().subscribe(confirmed => { if (confirmed) onConfirm(); });
  }

  private notify(message: string): void {
    this.snackBar.open(message, OPP_SNACKBAR_MODAL_LABEL_CLOSE, { duration: OPP_SNACKBAR_DURATION_DEFAULT * 2 });
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
