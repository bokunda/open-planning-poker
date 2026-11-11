import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, PLATFORM_ID, SimpleChanges, inject } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PokerCardComponent } from '../../../../shared/poker-card/poker-card.component';
import { PokerTableComponent } from '../table/poker-table.component';
import { VoteResultsComponent } from '../table/vote-results.component';
import { ApiCollectionOfGamePlayer, Settings, SettingsDetailsResult, Ticket, Vote } from '../../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-voting',
  templateUrl: './voting.component.html',
  styleUrl: './voting.component.scss',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatSelectModule, MatTooltipModule, PokerCardComponent, PokerTableComponent, VoteResultsComponent],
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

  /** True while the chosen card flies from the hand to the user's seat. */
  cardInFlight = false;
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  @Output() onCreateNewTicket = new EventEmitter<void>();
  @Output() onImportTickets = new EventEmitter<void>();
  @Output() OnVote = new EventEmitter<string>();
  @Output() onVoteAgain = new EventEmitter<void>();
  @Output() onRevealVotes = new EventEmitter<void>();
  @Output() onNavigateTicket = new EventEmitter<string>();

  voteOptions: string[] = [];
  selectedOption: string | null = null;
  descriptionExpanded = false;

  // Timer
  timerDurationMinutes = 1;
  remainingSeconds = this.timerDurationMinutes * 60;
  timerRunning = false;
  private timerInterval: ReturnType<typeof setInterval> | null = null;

  trackByOption(index: number, option: string): string {
    return option;
  }

  ngOnInit(): void {
    const settings = this.gameSettings as Settings;
    if (!settings) { return; }
    this.voteOptions = settings.deckSetup.split(',');
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['ticket'] && changes['ticket'].currentValue?.id !== changes['ticket'].previousValue?.id) {
      this.resetTimer();
      this.selectedOption = null;
      this.descriptionExpanded = false;
    }
    // A new voting round (re-vote) clears the card the user had chosen.
    if (changes['votesRevealed'] && changes['votesRevealed'].previousValue && !changes['votesRevealed'].currentValue) {
      this.selectedOption = null;
    }
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }

  selectOption(option: string, event?: Event): void {
    if (this.votesRevealed) return;
    const from = (event?.currentTarget as HTMLElement | null)?.querySelector('app-poker-card') as HTMLElement | null;
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
      this.descriptionExpanded = false;
    }
  }

  toggleDescription(): void {
    this.descriptionExpanded = !this.descriptionExpanded;
  }

  // --- Timer methods ---

  get timerDisplay(): string {
    const mins = Math.floor(this.remainingSeconds / 60);
    const secs = this.remainingSeconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  get timerWarning(): boolean {
    return this.timerRunning && this.remainingSeconds <= 10;
  }

  startTimer(): void {
    if (this.timerRunning) return;
    this.remainingSeconds = this.timerDurationMinutes * 60;
    this.timerRunning = true;
    this.timerInterval = setInterval(() => {
      this.remainingSeconds--;
      if (this.remainingSeconds <= 0) {
        this.clearTimer();
        this.onRevealVotes.emit();
      }
    }, 1000);
  }

  stopTimer(): void {
    this.clearTimer();
  }

  resetTimer(): void {
    this.clearTimer();
    this.remainingSeconds = this.timerDurationMinutes * 60;
  }

  private clearTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
    this.timerRunning = false;
  }
}
