import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { ApiCollectionOfGamePlayer, Settings, SettingsDetailsResult, Ticket, Vote } from '../../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-voting',
  templateUrl: './voting.component.html',
  styleUrl: './voting.component.scss',
  standalone: false,
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
  private readonly defaultTimerSeconds = 60;
  remainingSeconds = this.defaultTimerSeconds;
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
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }

  selectOption(option: string): void {
    this.selectedOption = option;
    this.OnVote.emit(option);
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
    this.remainingSeconds = this.defaultTimerSeconds;
  }

  private clearTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
    this.timerRunning = false;
  }
}
