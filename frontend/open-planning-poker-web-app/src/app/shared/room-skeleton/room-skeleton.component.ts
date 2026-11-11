import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Placeholder shaped like the game room (ticket, card hand, players panel), shown while
 * the game loads so the first frame is never empty. Rendered on the server too.
 */
@Component({
  selector: 'app-room-skeleton',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="skeleton" role="status" aria-live="polite">
      <span class="sr-only">Loading game…</span>
      <div class="skeleton-main" aria-hidden="true">
        <div class="sk sk-title"></div>
        <div class="sk sk-ticket"></div>
        <div class="sk-cards">
          <div class="sk sk-card" *ngFor="let c of cards"></div>
        </div>
        <div class="sk-actions">
          <div class="sk sk-pill"></div>
          <div class="sk sk-pill sk-pill--short"></div>
        </div>
      </div>
      <div class="skeleton-side" aria-hidden="true">
        <div class="sk sk-panel-head"></div>
        <div class="sk-player" *ngFor="let p of players">
          <div class="sk sk-avatar"></div>
          <div class="sk sk-name"></div>
          <div class="sk sk-mini-card"></div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .skeleton {
      display: grid;
      grid-template-columns: minmax(0, 1fr) var(--opp-sidebar-width);
      gap: var(--opp-space-6);
      width: 100%;
      max-width: 1200px;
      padding: var(--opp-space-6) var(--opp-space-4);
      box-sizing: border-box;
    }
    .skeleton-main, .skeleton-side { display: grid; gap: var(--opp-space-4); align-content: start; }
    .skeleton-main { justify-items: center; }
    .skeleton-side {
      background: var(--opp-surface);
      border: 1px solid var(--opp-border);
      border-radius: var(--opp-radius-lg);
      padding: var(--opp-space-4);
    }
    .sk {
      border-radius: var(--opp-radius-sm);
      background: linear-gradient(90deg,
        var(--opp-primary-50) 0 40%,
        color-mix(in srgb, var(--opp-primary) 16%, var(--opp-primary-50)) 50%,
        var(--opp-primary-50) 60% 100%);
      background-size: 300% 100%;
      animation: shimmer 1.6s linear infinite;
    }
    .sk-title { width: 40%; height: 28px; }
    .sk-ticket { width: 60%; height: 20px; }
    .sk-cards { display: flex; flex-wrap: wrap; justify-content: center; gap: var(--opp-space-3); padding-top: var(--opp-space-5); }
    .sk-card { width: 64px; height: 92px; border-radius: 12px; }
    .sk-actions { display: flex; gap: var(--opp-space-3); }
    .sk-pill { width: 140px; height: 40px; border-radius: var(--opp-radius-full); }
    .sk-pill--short { width: 110px; }
    .sk-panel-head { height: 22px; width: 50%; }
    .sk-player { display: grid; grid-template-columns: 32px 1fr 30px; gap: var(--opp-space-3); align-items: center; }
    .sk-avatar { width: 32px; height: 32px; border-radius: 50%; }
    .sk-name { height: 14px; }
    .sk-mini-card { width: 30px; height: 42px; }
    @keyframes shimmer { from { background-position: 100% 0; } to { background-position: 0 0; } }
    @media (max-width: 900px) {
      .skeleton { grid-template-columns: 1fr; }
      .sk-card { width: 54px; height: 78px; }
    }
  `],
})
export class RoomSkeletonComponent {
  readonly cards = Array.from({ length: 10 });
  readonly players = Array.from({ length: 4 });
}
