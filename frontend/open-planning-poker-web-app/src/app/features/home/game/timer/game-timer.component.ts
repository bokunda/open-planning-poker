import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, EventEmitter, Input, NgZone, OnInit, Output, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Apollo } from 'apollo-angular';
import type { DocumentNode } from 'graphql';
import { GameTimerState, GET_GAME_TIMER, ON_TIMER_CHANGED, START_TIMER, STOP_TIMER } from '../gql/timer.graphql';
import { TranslocoPipe } from '@jsverse/transloco';

/** What the table ring needs: seconds left (null when the timer is not running) and the full duration. */
export interface TimerTick { remaining: number | null; total: number; }

const DURATIONS = [30, 60, 120, 180, 300];

/**
 * Voting timer shared by everyone in the game. The host picks a duration and starts or stops it;
 * the server keeps the end time and pushes changes, so every player counts down to the same moment.
 * Players who are not the host see it only while it runs.
 */
@Component({
  selector: 'app-game-timer',
  imports: [TranslocoPipe],
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './game-timer.component.html',
  styleUrl: './game-timer.component.scss',
})
export class GameTimerComponent implements OnInit {
  @Input({ required: true }) gameId!: string;
  @Input() isHost = false;
  @Output() tick = new EventEmitter<TimerTick>();
  /** The countdown reached zero (not emitted when the host stops it). */
  @Output() expired = new EventEmitter<void>();

  readonly durations = DURATIONS;
  duration = 60;
  running = false;
  remaining = 60;
  busy = false;

  private endsAt = 0;
  /** Server clock minus client clock, so a client with a wrong clock still ends at the same moment. */
  private clockOffset = 0;
  private interval: ReturnType<typeof setInterval> | undefined;
  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  get display(): string {
    const s = this.running ? this.remaining : this.duration;
    return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
  }

  get warning(): boolean { return this.running && this.remaining <= 10; }

  label(seconds: number): string { return seconds < 60 ? `${seconds}s` : `${seconds / 60}m`; }

  ngOnInit(): void {
    this.destroyRef.onDestroy(() => this.clearTick());
    if (!this.isBrowser || !this.gameId) return;
    this.apollo.query<{ gameTimer: GameTimerState | null }>({ query: GET_GAME_TIMER, variables: { gameId: this.gameId }, fetchPolicy: 'no-cache' })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ data }) => { if (data?.gameTimer) this.apply(data.gameTimer); });
    this.apollo.subscribe<{ onTimerChanged: GameTimerState }>({ query: ON_TIMER_CHANGED, variables: { gameId: this.gameId } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ data }) => { if (data?.onTimerChanged) this.apply(data.onTimerChanged); });
  }

  pick(seconds: number): void {
    this.duration = seconds;
    this.emit();
  }

  start(): void {
    this.mutate(START_TIMER, { gameId: this.gameId, durationSeconds: this.duration }, 'startTimer');
  }

  stop(): void {
    this.mutate(STOP_TIMER, { gameId: this.gameId }, 'stopTimer');
  }

  /** Called by the room when the host moves to another ticket or reveals the cards. */
  stopIfRunning(): void {
    if (this.isHost && this.running) this.stop();
  }

  private mutate(mutation: DocumentNode, variables: Record<string, unknown>, field: 'startTimer' | 'stopTimer'): void {
    this.busy = true;
    this.cdr.markForCheck();
    this.apollo.mutate<Record<string, { gameTimer: GameTimerState | null }>>({ mutation, variables }).subscribe({
      next: ({ data }) => { const timer = data?.[field]?.gameTimer; if (timer) this.apply(timer); },
      error: () => this.done(),
      complete: () => this.done(),
    });
  }

  private done(): void {
    this.busy = false;
    this.cdr.markForCheck();
  }

  private apply(timer: GameTimerState): void {
    this.duration = timer.durationSeconds;
    this.clockOffset = new Date(timer.serverTime).getTime() - Date.now();
    this.endsAt = timer.endsAt ? new Date(timer.endsAt).getTime() : 0;
    this.running = timer.running && this.endsAt > this.now();
    this.clearTick();
    if (this.running) {
      this.remaining = this.secondsLeft();
      // Ticks outside Angular's zone; only this component re-renders.
      this.zone.runOutsideAngular(() => { this.interval = setInterval(() => this.update(), 250); });
    }
    this.emit();
    this.cdr.markForCheck();
  }

  private update(): void {
    const left = this.secondsLeft();
    if (left === this.remaining && left > 0) return;
    this.remaining = left;
    const ended = left === 0;
    if (ended) {
      this.running = false;
      this.clearTick();
    }
    this.zone.run(() => {
      this.emit();
      if (ended) this.expired.emit();
    });
    this.cdr.detectChanges();
  }

  private secondsLeft(): number { return Math.max(0, Math.ceil((this.endsAt - this.now()) / 1000)); }

  private emit(): void { this.tick.emit({ remaining: this.running ? this.remaining : null, total: this.duration }); }

  private now(): number { return Date.now() + this.clockOffset; }

  private clearTick(): void {
    if (this.interval) clearInterval(this.interval);
    this.interval = undefined;
  }
}
