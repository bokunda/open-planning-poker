import { ChangeDetectionStrategy, Component, EventEmitter, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';

const STORAGE_KEY = 'opp.inviteCardShownAt';
const WEEK_MS = 7 * 24 * 3600_000;

/** At most once a week per browser (OPP-174). Never throws: private mode or blocked storage just shows nothing. */
export function inviteCardAllowed(now = Date.now()): boolean {
  try {
    const last = Number(localStorage.getItem(STORAGE_KEY) ?? 0);
    return !last || now - last >= WEEK_MS;
  } catch {
    return false;
  }
}

export function markInviteCardShown(now = Date.now()): void {
  try {
    localStorage.setItem(STORAGE_KEY, String(now));
  } catch {
    /* storage blocked: the card may show again, which is harmless */
  }
}

/**
 * Offers a guest who just took part in a session to run one with their own team (OPP-174). Small, dismissable and
 * never shown during voting.
 */
@Component({
  selector: 'app-invite-card',
  standalone: true,
  imports: [TranslocoPipe, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="invite" role="complementary" [attr.aria-label]="'invite.title' | transloco">
      <div class="invite-text">
        <strong class="invite-title">{{ 'invite.title' | transloco }}</strong>
        <span class="invite-lead">{{ 'invite.lead' | transloco }}</span>
      </div>
      <div class="invite-actions">
        <button mat-flat-button color="primary" (click)="start.emit()">
          <mat-icon>add</mat-icon> {{ 'invite.start' | transloco }}
        </button>
        <button mat-icon-button (click)="dismiss.emit()" [attr.aria-label]="'common.close' | transloco">
          <mat-icon>close</mat-icon>
        </button>
      </div>
    </aside>
  `,
  styles: [`
    .invite { display: flex; align-items: center; justify-content: space-between; gap: var(--opp-space-3); flex-wrap: wrap;
      margin: var(--opp-space-4) 0; padding: var(--opp-space-3) var(--opp-space-4);
      border: 1px solid var(--opp-primary-200); border-radius: var(--opp-radius-lg);
      background: color-mix(in srgb, var(--opp-primary) 8%, var(--opp-surface)); }
    .invite-text { display: grid; gap: 2px; min-width: 0; }
    .invite-title { color: var(--opp-text); }
    .invite-lead { color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); }
    .invite-actions { display: flex; align-items: center; gap: var(--opp-space-1); }
  `],
})
export class InviteCardComponent {
  @Output() start = new EventEmitter<void>();
  @Output() dismiss = new EventEmitter<void>();
}
