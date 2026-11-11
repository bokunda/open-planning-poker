import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, OnDestroy, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';

export type PokerCardSize = 'sm' | 'md' | 'lg';

/**
 * A planning poker card: light face with the value, patterned violet back.
 * - `faceDown` shows the back; switching it to false flips the card (use `flipDelay` to stagger a reveal).
 * - `empty` draws a dashed slot for "no vote yet"; when it becomes a card, the card drops in.
 * Motion uses transform/opacity only and is disabled by the global reduced-motion rule.
 */
@Component({
  selector: 'app-poker-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './poker-card.component.html',
  styleUrl: './poker-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PokerCardComponent implements OnChanges, OnDestroy {
  @Input() value: string | null | undefined = '';
  @Input() size: PokerCardSize = 'md';
  @Input() faceDown = false;
  @Input() selected = false;
  @Input() empty = false;
  /** Delay before the flip starts, in ms (for revealing cards one after another). */
  @Input() flipDelay = 0;

  dropping = false;
  private readonly cdr = inject(ChangeDetectorRef);
  private dropTimer: ReturnType<typeof setTimeout> | undefined;

  get ariaLabel(): string {
    if (this.empty) return 'No vote yet';
    if (this.faceDown) return 'Voted, card hidden';
    return `Card ${this.value}`;
  }

  ngOnChanges(changes: SimpleChanges): void {
    const emptyChange = changes['empty'];
    if (emptyChange && !emptyChange.firstChange && emptyChange.previousValue && !emptyChange.currentValue) {
      this.dropping = true;
      clearTimeout(this.dropTimer);
      this.dropTimer = setTimeout(() => {
        this.dropping = false;
        this.cdr.markForCheck();
      }, 600);
    }
  }

  ngOnDestroy(): void {
    clearTimeout(this.dropTimer);
  }
}
