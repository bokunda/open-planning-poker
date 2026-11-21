import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, inject, Input, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Apollo } from 'apollo-angular';
import { DocumentNode } from 'graphql';
import { OPP_SNACKBAR_DURATION_DEFAULT, OPP_SNACKBAR_MODAL_LABEL_CLOSE } from '../../../shared/constants';
import { GET_GAME } from '../game/gql/getGame.graphql';
import { GET_GAME_PLAYERS } from '../game/gql/getPlayers.graphql';
import { JOIN_GAME } from '../game/gql/joinGame.graphql';
import { ON_GAME_CHANGED } from '../game/gql/room.graphql';
import { GameTimerComponent } from '../game/timer/game-timer.component';
import { RetroFactsComponent } from './retro-facts.component';
import {
  ADD_RETRO_ACTION_ITEM, ADD_RETRO_CARD, DELETE_RETRO_ACTION_ITEM, DELETE_RETRO_CARD, downloadBytes, EDIT_RETRO_CARD, factColumn,
  GET_RETRO, GROUP_RETRO_CARD, nextPhase, POST_RETRO_SUMMARY, Retro, RETRO_EXPORT, RETRO_PHASES, RETRO_TEMPLATES,
  RetroActionItem, RetroCard, RetroPhase, RetroStack, RetroTemplate, SET_RETRO_ACTION_ITEM_DONE, SET_RETRO_GROUP_TITLE,
  SET_RETRO_PHASE, stacksOf, START_RETRO, UPDATE_RETRO_ACTION_ITEM, UPDATE_RETRO_SETTINGS,
  VOTE_RETRO_CARD,
} from './retro.graphql';

type Payload = { errors?: { code: string; message: string }[] | null } | null | undefined;

interface Person { id: string; name: string }

/**
 * The retro of one team sprint (OPP-162): write in silence, reveal, group, vote with dots, then agree on action items.
 * It lives on the sprint's game, so joining works like joining the planning (guests included) and the timer is shared.
 */
@Component({
  selector: 'app-retro',
  standalone: true,
  imports: [TranslocoPipe, FormsModule, RouterLink, DragDropModule, MatButtonModule, MatIconModule, MatMenuModule, MatTooltipModule, GameTimerComponent, RetroFactsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './retro.component.html',
  styleUrl: './retro.component.scss',
})
export class RetroComponent implements OnInit {
  private _currentUserId = '';
  @Input()
  set currentUserId(value: string) {
    const first = !this._currentUserId && !!value;
    this._currentUserId = value;
    if (first && this.gameId) this.start();
  }
  get currentUserId(): string { return this._currentUserId; }

  private readonly apollo = inject(Apollo);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly phases = RETRO_PHASES;
  readonly templates = RETRO_TEMPLATES;
  readonly dotChoices = [1, 2, 3, 4, 5, 6, 8, 10];

  gameId = '';
  gameName = '';
  teamId: string | null = null;
  isGameHost = false;
  loading = true;
  notFound = false;
  retro: Retro | null = null;
  players: Person[] = [];
  chosenTemplate: RetroTemplate = 'went-well';
  drafts: string[] = [];
  editing: string | null = null;
  editText = '';
  titleDrafts: Record<string, string> = {};
  newItemText: Record<string, string> = {};
  newItemOwner: Record<string, string> = {};
  generalItemText = '';
  generalItemOwner = '';
  busy = false;
  private subscribed = false;

  ngOnInit(): void {
    this.gameId = this.route.snapshot.paramMap.get('gameId') ?? '';
    if (this.currentUserId && this.gameId) this.start();
  }

  // --- Loading ---

  private start(): void {
    this.apollo.mutate({ mutation: JOIN_GAME, variables: { input: { gameId: this.gameId } } }).subscribe({
      next: () => {
        this.loadGame();
        this.loadPlayers();
        this.load();
        this.subscribe();
      },
      error: () => { this.loading = false; this.notFound = true; this.cdr.markForCheck(); },
    });
  }

  private loadGame(): void {
    this.apollo.query<{ game: { id?: string; name?: string; hostId?: string; teamId?: string | null } }>({
      query: GET_GAME, variables: { id: this.gameId }, fetchPolicy: 'no-cache',
    }).subscribe(({ data }) => {
      const game = data?.game;
      if (!game?.id) { this.notFound = true; this.loading = false; this.cdr.markForCheck(); return; }
      this.gameName = game.name ?? '';
      this.teamId = game.teamId ?? null;
      this.isGameHost = game.hostId === this.currentUserId;
      this.cdr.markForCheck();
    });
  }

  private loadPlayers(): void {
    this.apollo.query<{ gamePlayers: { items?: { id: string; name: string | null; isCoHost?: boolean }[] } }>({
      query: GET_GAME_PLAYERS, variables: { gameId: this.gameId }, fetchPolicy: 'no-cache',
    }).subscribe(({ data }) => {
      const items = data?.gamePlayers?.items ?? [];
      this.players = items.map(p => ({ id: p.id, name: p.name ?? '' }));
      if (items.some(p => p.id === this.currentUserId && p.isCoHost)) this.isGameHost = true;
      this.cdr.markForCheck();
    });
  }

  load(): void {
    this.apollo.query<{ retro: Retro & { code?: string } }>({ query: GET_RETRO, variables: { gameId: this.gameId }, fetchPolicy: 'no-cache' })
      .subscribe({
        next: ({ data }) => {
          this.loading = false;
          const retro = data?.retro;
          this.retro = retro?.id ? retro : null;
          if (retro?.code && retro.code !== 'RetroNotFound') this.notFound = true;
          if (this.retro && this.drafts.length !== this.retro.columns) this.drafts = Array(this.retro.columns).fill('');
          this.cdr.markForCheck();
        },
        error: () => { this.loading = false; this.notFound = true; this.cdr.markForCheck(); },
      });
  }

  private subscribe(): void {
    if (this.subscribed) return;
    this.subscribed = true;
    this.apollo.subscribe<{ onGameChanged: { kind: string } }>({ query: ON_GAME_CHANGED, variables: { gameId: this.gameId } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ data }) => {
        switch (data?.onGameChanged?.kind) {
          case 'RETRO': this.load(); break;
          case 'PLAYERS': this.loadPlayers(); break;
        }
      });
  }

  // --- View helpers ---

  get phaseIndex(): number { return this.retro ? RETRO_PHASES.indexOf(this.retro.phase) : 0; }
  get next(): RetroPhase | null { return this.retro ? nextPhase(this.retro.phase) : null; }
  get canWrite(): boolean { return this.retro?.phase === 'WRITING' || this.retro?.phase === 'GROUPING'; }
  get isGrouping(): boolean { return this.retro?.phase === 'GROUPING'; }
  get isVoting(): boolean { return this.retro?.phase === 'VOTING'; }
  get isDiscussing(): boolean { return this.retro?.phase === 'DISCUSSING'; }
  get columnIndexes(): number[] { return this.retro ? Array.from({ length: this.retro.columns }, (_, i) => i) : []; }
  get canStart(): boolean { return this.isGameHost; }

  templateColumns(template: RetroTemplate): number[] {
    return template === '4ls' ? [0, 1, 2, 3] : [0, 1, 2];
  }

  columnTitle(column: number): string {
    return this.retro ? `retro.templates.${this.retro.template}.${column}` : '';
  }

  stacks(column: number): RetroStack[] {
    return this.retro ? stacksOf(this.retro.cards, column) : [];
  }

  /** Cards others wrote in a column while writing: only the number is known. */
  hiddenCount(column: number): number {
    if (!this.retro || this.retro.phase !== 'WRITING') return 0;
    const mine = this.retro.cards.filter(c => c.column === column).length;
    return Math.max(0, (this.retro.countPerColumn[column] ?? 0) - mine);
  }

  nameOf(id: string | null): string {
    return id ? this.players.find(p => p.id === id)?.name ?? '' : '';
  }

  /** Heads of other stacks a card can join (the ⋮ menu on phones). */
  targetsFor(card: RetroCard): RetroCard[] {
    if (!this.retro) return [];
    const headId = card.groupId ?? card.id;
    return this.retro.cards.filter(c => !c.groupId && c.id !== headId);
  }

  itemsFor(cardId: string): RetroActionItem[] {
    return this.retro?.actionItems.filter(i => i.cardId === cardId) ?? [];
  }

  get generalItems(): RetroActionItem[] {
    const ids = new Set(this.retro?.cards.map(c => c.id) ?? []);
    return this.retro?.actionItems.filter(i => !i.cardId || !ids.has(i.cardId)) ?? [];
  }

  dropListId(cardId: string): string { return `retro-stack-${cardId}`; }

  get dropListIds(): string[] {
    return [...(this.retro?.cards.filter(c => !c.groupId).map(c => this.dropListId(c.id)) ?? []), ...this.columnIndexes.map(i => `retro-col-${i}`)];
  }

  maxVotes(): number {
    return Math.max(1, ...(this.retro?.cards.map(c => c.votes ?? 0) ?? [0]));
  }

  // --- Host ---

  startRetro(): void {
    if (!this.teamId || this.busy) return;
    this.busy = true;
    this.run(START_RETRO, 'startRetro', { teamId: this.teamId, gameId: this.gameId, template: this.chosenTemplate }, () => this.load(),
      () => { this.busy = false; });
  }

  advance(): void {
    const phase = this.next;
    if (!this.retro || !phase) return;
    this.run(SET_RETRO_PHASE, 'setRetroPhase', { retroId: this.retro.id, phase }, () => this.load());
  }

  updateSettings(showAuthors: boolean, dotsPerPerson: number): void {
    if (!this.retro) return;
    this.run(UPDATE_RETRO_SETTINGS, 'updateRetroSettings', { retroId: this.retro.id, showAuthors, dotsPerPerson }, () => this.load());
  }

  postSummary(): void {
    if (!this.retro) return;
    this.run(POST_RETRO_SUMMARY, 'postRetroSummary', { retroId: this.retro.id }, () => this.notify(this.i18n.translate('retro.posted')));
  }

  // --- Cards ---

  addCard(column: number): void {
    const text = (this.drafts[column] ?? '').trim();
    if (!this.retro || !text) return;
    this.drafts[column] = '';
    this.run(ADD_RETRO_CARD, 'addRetroCard', { retroId: this.retro.id, column, text }, () => this.load(),
      () => { this.drafts[column] = text; });
  }

  /** One click turns a sprint fact into a card in the "to improve" column (OPP-167). */
  addFact(text: string): void {
    if (!this.retro || !this.canWrite) return;
    this.run(ADD_RETRO_CARD, 'addRetroCard', { retroId: this.retro.id, column: factColumn(this.retro.template), text }, () => {
      this.load();
      this.notify(this.i18n.translate('retro.facts.added'));
    });
  }

  startEdit(card: RetroCard): void {
    this.editing = card.id;
    this.editText = card.text;
  }

  saveEdit(card: RetroCard): void {
    const text = this.editText.trim();
    this.editing = null;
    if (!text || text === card.text) return;
    this.run(EDIT_RETRO_CARD, 'editRetroCard', { cardId: card.id, text }, () => this.load());
  }

  deleteCard(card: RetroCard): void {
    this.run(DELETE_RETRO_CARD, 'deleteRetroCard', { cardId: card.id }, () => this.load());
  }

  group(card: RetroCard, target: RetroCard | null): void {
    this.run(GROUP_RETRO_CARD, 'groupRetroCard', { cardId: card.id, targetCardId: target?.id ?? null }, () => this.load());
  }

  /** Dropped on a stack: join it. Dropped on the column: leave the group. */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  drop(event: CdkDragDrop<any>): void {
    const card = event.item.data as RetroCard;
    if (typeof event.container.data === 'number') {
      if (card.groupId) this.group(card, null);
      return;
    }
    const target = event.container.data as RetroCard;
    if (target.id === card.id || target.id === card.groupId) return;
    this.group(card, target);
  }

  saveTitle(head: RetroCard): void {
    const title = (this.titleDrafts[head.id] ?? '').trim();
    if (title === (head.groupTitle ?? '')) return;
    this.run(SET_RETRO_GROUP_TITLE, 'setRetroGroupTitle', { cardId: head.id, title: title || null }, () => this.load());
  }

  titleDraft(head: RetroCard): string {
    return this.titleDrafts[head.id] ?? head.groupTitle ?? '';
  }

  // --- Dots ---

  vote(head: RetroCard, add: boolean): void {
    if (!this.retro || (add && this.retro.dotsLeft <= 0) || (!add && head.myVotes <= 0)) return;
    // Shown at once; the server answers with the dots left.
    head.myVotes += add ? 1 : -1;
    this.retro.dotsLeft += add ? -1 : 1;
    this.apollo.mutate<{ voteRetroCard: Payload & { int?: number } }>({
      mutation: VOTE_RETRO_CARD, variables: { input: { cardId: head.id, add } },
    }).subscribe({
      next: ({ data }) => {
        const result = data?.voteRetroCard;
        if (result?.errors?.length) {
          this.notify(result.errors[0].message);
          this.load();
          return;
        }
        if (this.retro && typeof result?.int === 'number') this.retro.dotsLeft = result.int;
        this.cdr.markForCheck();
      },
      error: () => this.load(),
    });
  }

  dots(n: number): number[] { return Array.from({ length: n }, (_, i) => i); }

  // --- Action items ---

  addItem(cardId: string | null): void {
    if (!this.retro) return;
    const text = (cardId ? this.newItemText[cardId] : this.generalItemText)?.trim();
    const owner = (cardId ? this.newItemOwner[cardId] : this.generalItemOwner) || null;
    if (!text) return;
    this.run(ADD_RETRO_ACTION_ITEM, 'addRetroActionItem', { retroId: this.retro.id, text, cardId, ownerId: owner }, () => {
      if (cardId) { this.newItemText[cardId] = ''; this.newItemOwner[cardId] = ''; }
      else { this.generalItemText = ''; this.generalItemOwner = ''; }
      this.load();
    });
  }

  setOwner(item: RetroActionItem, ownerId: string): void {
    this.run(UPDATE_RETRO_ACTION_ITEM, 'updateRetroActionItem', { actionItemId: item.id, text: item.text, ownerId: ownerId || null }, () => this.load());
  }

  setDone(item: RetroActionItem, done: boolean): void {
    this.run(SET_RETRO_ACTION_ITEM_DONE, 'setRetroActionItemDone', { actionItemId: item.id, done }, () => this.load());
  }

  deleteItem(item: RetroActionItem): void {
    this.run(DELETE_RETRO_ACTION_ITEM, 'deleteRetroActionItem', { actionItemId: item.id }, () => this.load());
  }

  // --- Export (OPP-168) ---

  exportFile(pdf: boolean): void {
    if (!this.retro) return;
    this.apollo.query<{ retroExport: { fileName?: string; contentType?: string; data?: number[] | string; message?: string } }>({
      query: RETRO_EXPORT, variables: { retroId: this.retro.id, pdf }, fetchPolicy: 'no-cache',
    }).subscribe({
      next: ({ data }) => {
        const file = data?.retroExport;
        if (!file?.data || !file.fileName) { this.notify(file?.message ?? this.i18n.translate('common.tryAgainNetwork')); return; }
        downloadBytes(file.data, file.fileName, file.contentType ?? 'application/octet-stream');
      },
      error: () => this.notify(this.i18n.translate('common.tryAgainNetwork')),
    });
  }

  get shareUrl(): string {
    return typeof window === 'undefined' ? '' : `${window.location.origin}/retro/${this.gameId}`;
  }

  copyLink(): void {
    navigator.clipboard?.writeText(this.shareUrl).then(() => this.notify(this.i18n.translate('retro.linkCopied')));
  }

  // --- Plumbing ---

  private run(mutation: DocumentNode, field: string, input: Record<string, unknown>, onSuccess?: () => void, onError?: () => void): void {
    this.apollo.mutate<Record<string, Payload>>({ mutation, variables: { input } }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        const errors = data?.[field]?.errors;
        if (errors?.length) {
          this.notify(errors[0].message);
          onError?.();
          this.cdr.markForCheck();
          return;
        }
        onSuccess?.();
      },
      error: () => {
        this.busy = false;
        this.notify(this.i18n.translate('common.tryAgainNetwork'));
        onError?.();
        this.cdr.markForCheck();
      },
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, OPP_SNACKBAR_MODAL_LABEL_CLOSE, { duration: OPP_SNACKBAR_DURATION_DEFAULT });
  }

  trackStack = (_: number, s: RetroStack) => s.head.id;
  trackItem = (_: number, i: RetroActionItem) => i.id;
}
