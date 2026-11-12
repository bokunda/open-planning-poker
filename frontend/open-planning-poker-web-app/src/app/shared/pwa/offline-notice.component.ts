import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PwaService } from './pwa.service';

/** Friendly full-screen notice while the device is offline; the app reconnects on its own. */
@Component({
  selector: 'app-offline-notice',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!pwa.online()) {
      <div class="offline" role="alertdialog" aria-modal="true" aria-labelledby="offline-title" aria-describedby="offline-desc">
        <div class="offline-card">
          <svg class="offline-icon" viewBox="0 0 24 24" width="40" height="40" fill="none" stroke="currentColor"
            stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M2 8.8a15 15 0 0 1 4.2-2.6M8.5 5.3A15 15 0 0 1 22 8.8M5 12.6a10 10 0 0 1 3.1-2M11.6 9.1A10 10 0 0 1 19 12.6M8.5 16.4a5 5 0 0 1 6.2-.6M12 20h.01M3 3l18 18"/>
          </svg>
          <h2 class="offline-title" id="offline-title">You are offline</h2>
          <p class="offline-desc" id="offline-desc">
            Planning poker needs a connection to vote with your team. Open Planning Poker reconnects on its own
            as soon as you are back online.
          </p>
        </div>
      </div>
    }
  `,
  styles: [`
    .offline { position: fixed; inset: 0; z-index: 2000; display: grid; place-items: center; padding: 16px;
      background: color-mix(in srgb, var(--opp-bg, #fff) 88%, transparent); backdrop-filter: blur(4px); }
    .offline-card { max-width: 420px; padding: 28px 24px; border-radius: 16px; text-align: center;
      background: var(--opp-surface, #fff); color: var(--opp-text, #1c1b22); border: 1px solid var(--opp-border, #e4e2ec);
      box-shadow: 0 12px 40px rgb(0 0 0 / 0.12); }
    .offline-icon { color: var(--opp-primary, #6a3ae8); }
    .offline-title { margin: 12px 0 8px; font-size: 1.375rem; }
    .offline-desc { margin: 0; line-height: 1.5; color: var(--opp-text-secondary, #55525f); }
  `],
})
export class OfflineNoticeComponent {
  protected readonly pwa = inject(PwaService);
}
