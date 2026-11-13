import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnDestroy, OnInit, Output, PLATFORM_ID, QueryList, SimpleChanges, ViewChild, ViewChildren, inject } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PokerCardComponent } from '../../../../shared/poker-card/poker-card.component';
import { PokerTableComponent } from '../table/poker-table.component';
import { VoteResultsComponent } from '../table/vote-results.component';
import { GameTimerComponent, TimerTick } from '../timer/game-timer.component';
import { ApiCollectionOfGamePlayer, Settings, SettingsDetailsResult, Ticket, Vote } from '../../../../graphql/graphql-gateway.service';
import { MarkdownPipe } from '../../../../shared/markdown/markdown.pipe';
import { RoomSettings, RoomTicket } from '../room-types';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-voting',
  templateUrl: './voting.component.html',
  styleUrl: './voting.component.scss',
  standalone: true,
  imports: [TranslocoPipe, CommonModule, MatButtonModule, MatIconModule, MatTooltipModule, PokerCardComponent, PokerTableComponent, VoteResultsComponent, GameTimerComponent, MarkdownPipe],
  changeDetection: ChangeDetectionStrategy.Default
})
export class VotingComponent implements OnInit, OnDestroy, OnChanges {

  @Input() gameSettings: SettingsDetailsResult | undefined;
  @Input() players: ApiCollectionOfGamePlayer | undefined;
  @Input() ticket: Ticket | undefined;
  @Input() tickets: Ticket[] = [];
  @Input() votes: Vote[] = [];
  @Input() votesRevealed = false;
  @Input() isHost = false;
  @Input() currentUserId = '';
  /** Observers watch the table and get no cards. */
  @Input() isObserver = false;

  /** True while the chosen card flies from the hand to the user's seat. */
  cardInFlight = false;

  @ViewChildren('voteButton') private voteButtons!: QueryList<ElementRef<HTMLButtonElement>>;
  private typedKeys = '';
  private typedKeysTimer: ReturnType<typeof setTimeout> | undefined;

  /** The one card in the hand that is reachable with Tab (roving tabindex). */
  get tabStopIndex(): number {
    const selected = this.selectedOption ? this.voteOptions.indexOf(this.selectedOption) : -1;
    return selected >= 0 ? selected : 0;
  }
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly i18n = inject(TranslocoService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  @Output() onCreateNewTicket = new EventEmitter<void>();
  @Output() onImportTickets = new EventEmitter<void>();
  @Output() OnVote = new EventEmitter<string>();
  @Output() onVoteAgain = new EventEmitter<void>();
  @Output() onRevealVotes = new EventEmitter<void>();
  @Output() onNavigateTicket = new EventEmitter<string>();
  @Output() onSetFinalEstimate = new EventEmitter<string | null>();

  private playersSource: ApiCollectionOfGamePlayer | undefined;
  private seatedCache: ApiCollectionOfGamePlayer | undefined;

  /** Players at the table: observers watch, so they get no seat (same object until the list changes). */
  get seatedPlayers(): ApiCollectionOfGamePlayer | undefined {
    if (this.players !== this.playersSource) {
      this.playersSource = this.players;
      const items = (this.players?.items ?? []).filter(p => (p as { role?: string }).role !== 'OBSERVER');
      this.seatedCache = this.players ? { ...this.players, items, totalCount: items.length } : undefined;
    }
    return this.seatedCache;
  }

  get autoReveal(): boolean {
    return !!(this.gameSettings as RoomSettings | undefined)?.autoReveal;
  }

  get finalEstimate(): string | null {
    return (this.ticket as RoomTicket | undefined)?.finalEstimate ?? null;
  }

  voteOptions: string[] = [];
  selectedOption: string | null = null;
  /** The ticket description is shown by default; the info button hides it. */
  descriptionExpanded = true;

  // Timer: shared by everyone, run by the server (app-game-timer); the table ring follows it.
  @Input() gameId = '';
  @ViewChild(GameTimerComponent) private timer?: GameTimerComponent;
  timerTick: TimerTick = { remaining: null, total: 60 };

  trackByOption(index: number, option: string): string {
    return option;
  }

  ngOnInit(): void {
    const settings = this.gameSettings as Settings;
    if (!settings) { return; }
    this.voteOptions = settings.deckSetup.split(',');
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['gameSettings'] && !changes['gameSettings'].firstChange) {
      const deck = (this.gameSettings as Settings | undefined)?.deckSetup;
      if (deck) this.voteOptions = deck.split(',');
    }
    const ticketChanged = changes['ticket'] && changes['ticket'].currentValue?.id !== changes['ticket'].previousValue?.id;
    if (ticketChanged) {
      if (!changes['ticket'].firstChange) this.timer?.stopIfRunning();
      this.selectedOption = null;
      this.descriptionExpanded = true;
      if (!changes['ticket'].firstChange && this.ticket?.name) {
        this.announce(this.i18n.translate('voting.announceTicket', { name: this.ticket.name }));
      }
    }
    // A new voting round (re-vote) clears the card the user had chosen.
    if (changes['votesRevealed']?.currentValue && !changes['votesRevealed'].firstChange) {
      this.timer?.stopIfRunning();
    }
    const newRound = changes['votesRevealed'] && changes['votesRevealed'].previousValue && !changes['votesRevealed'].currentValue;
    if (newRound) {
      this.selectedOption = null;
      if (!ticketChanged) this.announce(this.i18n.translate('voting.announceRound'));
    }
    // Someone voted (only before the reveal; values stay hidden).
    const votesChange = changes['votes'];
    if (votesChange && !votesChange.firstChange && !this.votesRevealed && !ticketChanged && !newRound) {
      const before = new Set((votesChange.previousValue as Vote[] | undefined ?? []).map(v => v.playerId));
      const arrived = (votesChange.currentValue as Vote[]).filter(v => !before.has(v.playerId));
      if (arrived.length) {
        const total = this.players?.items?.length ?? 0;
        const who = arrived.map(v => this.playerName(v.playerId)).join(', ');
        this.announce(this.i18n.translate('voting.announceVoted', { who, count: this.votes.length, total }));
      }
    }
  }

  ngOnDestroy(): void {
    clearTimeout(this.typedKeysTimer);
    clearTimeout(this.announceTimer);
  }

  /** Text for the screen-reader live region (votes arriving, new rounds, ticket changes). */
  announcement = '';
  private announceTimer: ReturnType<typeof setTimeout> | undefined;

  private announce(message: string): void {
    // Clear first so the same sentence is read again when it repeats.
    this.announcement = '';
    clearTimeout(this.announceTimer);
    this.announceTimer = setTimeout(() => {
      this.announcement = message;
      this.cdr.detectChanges();
    }, 50);
  }

  private playerName(playerId: string): string {
    if (playerId === this.currentUserId) return this.i18n.translate('common.you');
    return this.players?.items?.find(p => p.id === playerId)?.name ?? this.i18n.translate('results.aPlayer');
  }

  /** Arrow keys, Home and End move focus inside the hand; Enter/Space vote (native buttons). */
  onHandKeydown(event: KeyboardEvent): void {
    const buttons = this.voteButtons?.toArray().map(b => b.nativeElement) ?? [];
    const current = buttons.indexOf(document.activeElement as HTMLButtonElement);
    if (current < 0) return;
    let next = current;
    switch (event.key) {
      case 'ArrowRight': case 'ArrowDown': next = (current + 1) % buttons.length; break;
      case 'ArrowLeft': case 'ArrowUp': next = (current - 1 + buttons.length) % buttons.length; break;
      case 'Home': next = 0; break;
      case 'End': next = buttons.length - 1; break;
      default: return;
    }
    event.preventDefault();
    buttons[next].focus();
  }

  /**
   * Typing a card's value votes for it ("5", "13", "0.5", "?"), unless the user is typing in a
   * field or a dialog is open. Multi-character values are collected for a short moment.
   */
  @HostListener('document:keydown', ['$event'])
  onShortcut(event: KeyboardEvent): void {
    if (!this.ticket?.id || this.votesRevealed || this.isObserver || event.ctrlKey || event.metaKey || event.altKey) return;
    const target = event.target as HTMLElement | null;
    if (target?.closest('input, textarea, select, [contenteditable="true"], .cdk-overlay-container')) return;
    if (event.key.length !== 1 || !/[0-9.?½]/.test(event.key)) return;

    clearTimeout(this.typedKeysTimer);
    this.typedKeys += event.key;
    const exact = this.cardForKeys(this.typedKeys);
    const longer = this.voteOptions.some(v => v.startsWith(this.typedKeys) && v !== this.typedKeys);
    if (exact && !longer) {
      this.voteWithKeyboard(exact);
      this.typedKeys = '';
      return;
    }
    // Wait briefly for a second key ("1" could become "13"), then take the exact match if any.
    this.typedKeysTimer = setTimeout(() => {
      const card = this.cardForKeys(this.typedKeys);
      if (card) this.voteWithKeyboard(card);
      this.typedKeys = '';
      this.cdr.detectChanges();
    }, 600);
  }

  private cardForKeys(keys: string): string | undefined {
    if (keys === '?') return this.voteOptions.find(v => v.includes('?') || v.includes('❓'));
    return this.voteOptions.find(v => v === keys || (keys === '.5' && v === '0.5'));
  }

  private voteWithKeyboard(option: string): void {
    const index = this.voteOptions.indexOf(option);
    const button = this.voteButtons?.get(index)?.nativeElement;
    this.selectOption(option, button);
    button?.focus({ preventScroll: true });
  }

  selectOption(option: string, source?: Event | HTMLElement): void {
    if (this.votesRevealed || this.isObserver) return;
    const origin = source instanceof HTMLElement ? source : (source?.currentTarget as HTMLElement | null);
    const from = origin?.querySelector('app-poker-card') as HTMLElement | null;
    this.selectedOption = option;
    this.OnVote.emit(option);
    if (from) this.flyCardToSeat(from, option);
  }

  /**
   * FLIP animation: a copy of the chosen card travels from the hand to the user's seat.
   * The seat card stays hidden until the copy lands. Skipped for reduced motion and on the server.
   */
  private flyCardToSeat(from: HTMLElement, value: string): void {
    if (!this.isBrowser || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    const seat = this.host.nativeElement.querySelector(`[data-seat-player="${this.currentUserId}"] app-poker-card`) as HTMLElement | null;
    if (!seat || typeof from.animate !== 'function') return;

    const a = from.getBoundingClientRect();
    const b = seat.getBoundingClientRect();
    const flight = document.createElement('div');
    flight.textContent = value;
    flight.setAttribute('aria-hidden', 'true');
    Object.assign(flight.style, {
      position: 'fixed', left: `${a.left}px`, top: `${a.top}px`, width: `${a.width}px`, height: `${a.height}px`,
      zIndex: '1000', pointerEvents: 'none', display: 'grid', placeItems: 'center', borderRadius: '12px',
      background: 'var(--opp-card-face)', color: 'var(--opp-card-ink)', border: '2px solid var(--opp-primary)',
      fontFamily: 'var(--opp-font-display)', fontWeight: '800', fontSize: '1.6rem',
      boxShadow: 'var(--opp-shadow-card-lifted)', transformOrigin: 'center',
    } as Partial<CSSStyleDeclaration>);
    document.body.appendChild(flight);

    const dx = b.left + b.width / 2 - (a.left + a.width / 2);
    const dy = b.top + b.height / 2 - (a.top + a.height / 2);
    const scale = b.width / a.width;
    this.cardInFlight = true;
    const animation = flight.animate([
      { transform: 'translate(0, 0) rotate(0deg) scale(1)' },
      { transform: `translate(${dx * 0.5}px, ${dy * 0.5 - 48}px) rotate(-12deg) scale(${(1 + scale) / 2})`, offset: 0.55 },
      { transform: `translate(${dx}px, ${dy}px) rotate(0deg) scale(${scale})` },
    ], { duration: 460, easing: 'cubic-bezier(0.2, 0.8, 0.2, 1)' });
    const land = () => {
      flight.remove();
      this.cardInFlight = false;
      // onfinish can run outside Angular's zone, so render the change explicitly.
      this.cdr.detectChanges();
    };
    animation.onfinish = land;
    animation.oncancel = land;
  }

  handleOnCreateNewTicket() {
    this.onCreateNewTicket.emit();
  }

  handleOnImportTickets() {
    this.onImportTickets.emit();
  }

  get currentTicketIndex(): number {
    return this.tickets.findIndex(t => t.id === this.ticket?.id);
  }

  get hasPrevTicket(): boolean {
    return this.currentTicketIndex > 0;
  }

  get hasNextTicket(): boolean {
    return this.currentTicketIndex >= 0 && this.currentTicketIndex < this.tickets.length - 1;
  }

  get prevTicketId(): string | undefined {
    return this.hasPrevTicket ? this.tickets[this.currentTicketIndex - 1]?.id : undefined;
  }

  get nextTicketId(): string | undefined {
    return this.hasNextTicket ? this.tickets[this.currentTicketIndex + 1]?.id : undefined;
  }

  navigateToTicket(ticketId: string | undefined): void {
    if (ticketId) {
      this.onNavigateTicket.emit(ticketId);
      this.descriptionExpanded = true;
    }
  }

  toggleDescription(): void {
    this.descriptionExpanded = !this.descriptionExpanded;
  }

  // --- Timer ---

  onTimerTick(tick: TimerTick): void {
    this.timerTick = tick;
  }

  /** When the countdown ends, the host's client reveals the cards (as before). */
  onTimerExpired(): void {
    if (this.isHost && !this.votesRevealed) this.onRevealVotes.emit();
  }
}
