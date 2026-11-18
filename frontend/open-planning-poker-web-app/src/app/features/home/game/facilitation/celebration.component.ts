import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, OnDestroy, SimpleChanges, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

interface Piece {
  left: number;
  delay: number;
  duration: number;
  drift: number;
  spin: number;
  color: string;
  wide: boolean;
}

const COLORS = ['#6a3ae8', '#f4b740', '#2fb67c', '#ef5a7a', '#3aa7e8', '#ffffff'];
const PIECES = 36;
const DURATION_MS = 1800;

/**
 * A short confetti moment when everyone voted the same card (OPP-172). Increase `burst` to play it once. Uses only
 * transform/opacity, never blocks clicks, and plays nothing for people who ask for reduced motion (they still get
 * the spoken note). From the third consensus in a row it also says how long the streak is.
 */
@Component({
  selector: 'app-celebration',
  standalone: true,
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (active) {
      @if (!reducedMotion) {
        <div class="confetti" aria-hidden="true">
          @for (p of pieces; track $index) {
            <span class="piece" [class.piece--wide]="p.wide"
              [style.left.%]="p.left" [style.background]="p.color"
              [style.animation-delay.ms]="p.delay" [style.animation-duration.ms]="p.duration"
              [style.--drift.px]="p.drift" [style.--spin.deg]="p.spin"></span>
          }
        </div>
      }
      @if (streak >= 3) {
        <div class="streak">{{ 'celebration.streak' | transloco: { count: streak } }}</div>
      }
    }
    <!-- Stays until the next consensus, so the announcement is not cut off by the end of the animation. -->
    <span class="sr-only" aria-live="polite">{{ announced ? ((streak >= 3 ? 'celebration.streak' : 'celebration.consensus') | transloco: { count: streak }) : '' }}</span>
  `,
  styles: [`
    :host { position: fixed; inset: 0; pointer-events: none; z-index: var(--opp-z-overlay, 900); overflow: hidden; }
    .confetti { position: absolute; inset: 0; }
    .piece { position: absolute; top: -12px; width: 8px; height: 12px; border-radius: 2px; opacity: 0;
      animation-name: fall; animation-timing-function: cubic-bezier(.2,.6,.4,1); animation-fill-mode: forwards; will-change: transform, opacity; }
    .piece--wide { width: 12px; height: 7px; }
    @keyframes fall {
      0% { opacity: 1; transform: translate3d(0, 0, 0) rotate(0); }
      85% { opacity: 1; }
      100% { opacity: 0; transform: translate3d(var(--drift), 70vh, 0) rotate(var(--spin)); }
    }
    .streak { position: absolute; top: 18%; left: 50%; transform: translateX(-50%);
      padding: var(--opp-space-2) var(--opp-space-4); border-radius: var(--opp-radius-full);
      background: var(--opp-primary); color: var(--opp-text-on-primary); font-weight: 700; box-shadow: var(--opp-shadow-lg);
      animation: pop 1.8s ease-out forwards; }
    @keyframes pop { 0% { opacity: 0; transform: translate(-50%, 8px) scale(.9); } 15%, 80% { opacity: 1; transform: translate(-50%, 0) scale(1); } 100% { opacity: 0; } }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
    @media (prefers-reduced-motion: reduce) { .streak { animation: none; } }
  `],
})
export class CelebrationComponent implements OnChanges, OnDestroy {
  /** Each new value plays the celebration once. */
  @Input() burst = 0;
  /** Consensus tickets in a row, including this one. */
  @Input() streak = 1;

  active = false;
  announced = false;
  pieces: Piece[] = [];
  readonly reducedMotion = typeof window !== 'undefined' && !!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

  private readonly cdr = inject(ChangeDetectorRef);
  private timer: ReturnType<typeof setTimeout> | undefined;

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['burst'] || !this.burst) return;
    this.pieces = Array.from({ length: PIECES }, (_, i) => ({
      left: Math.random() * 100,
      delay: Math.random() * 250,
      duration: 1100 + Math.random() * 500,
      drift: (Math.random() - 0.5) * 160,
      spin: (Math.random() - 0.5) * 720,
      color: COLORS[i % COLORS.length],
      wide: i % 3 === 0,
    }));
    this.active = true;
    this.announced = true;
    clearTimeout(this.timer);
    this.timer = setTimeout(() => {
      this.active = false;
      this.cdr.markForCheck();
    }, DURATION_MS);
  }

  ngOnDestroy(): void {
    clearTimeout(this.timer);
  }
}

/** Everyone voted, at least two players, and they picked the same sizing card (not "?" or a break). */
export function isConsensus(values: (string | null | undefined)[]): boolean {
  const cards = values.filter((v): v is string => !!v);
  if (cards.length < 2 || cards.length !== values.length) return false;
  const first = cards[0];
  if (/[?❓☕⏸]/u.test(first)) return false;
  return cards.every(v => v === first);
}
