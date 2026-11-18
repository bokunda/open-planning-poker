import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Apollo } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import { OPP_SNACKBAR_DURATION_DEFAULT } from '../../shared/constants';
import { trackEvent } from '../../shared/analytics';
import { RECAP_SHARING, RecapSharing, recapUrl, SET_RECAP_SHARING } from './recap.graphql';

export interface RecapShareDialogData {
  gameId: string;
}

type ApiError = { code: string; message: string };

/**
 * Shares the recap of a session as a read-only link (OPP-170). The host can include the names of the players and
 * stop sharing at any time; the link then stops working.
 */
@Component({
  selector: 'app-recap-share-dialog',
  standalone: true,
  imports: [TranslocoPipe, MatDialogModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ 'recap.shareTitle' | transloco }}</h2>
    <mat-dialog-content>
      <p class="share-lead">{{ 'recap.shareLead' | transloco }}</p>
      @if (loading) {
        <p class="share-muted">{{ 'common.loading' | transloco }}</p>
      } @else if (sharing?.shared && sharing?.token) {
        <label class="share-label" for="recap-link">{{ 'recap.link' | transloco }}</label>
        <div class="share-link">
          <input id="recap-link" class="share-input" readonly [value]="link" (focus)="select($event)">
          <button mat-flat-button color="primary" (click)="copy()">
            <mat-icon>content_copy</mat-icon> {{ 'recap.copy' | transloco }}
          </button>
        </div>
        <a class="share-open" [href]="link" target="_blank" rel="noopener">{{ 'recap.openLink' | transloco }}</a>
        <label class="share-option">
          <input type="checkbox" [checked]="sharing!.showNames" [disabled]="busy" (change)="setShared(true, !sharing!.showNames)">
          {{ 'recap.showNames' | transloco }}
        </label>
      } @else {
        <label class="share-option">
          <input type="checkbox" [checked]="showNames" (change)="showNames = !showNames">
          {{ 'recap.showNames' | transloco }}
        </label>
        <p class="share-muted">{{ 'recap.privacy' | transloco }}</p>
      }
      @if (error) { <p class="share-error" role="alert">{{ error }}</p> }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      @if (sharing?.shared) {
        <button mat-button [disabled]="busy" (click)="setShared(false, false)">{{ 'recap.stop' | transloco }}</button>
        <button mat-flat-button color="primary" mat-dialog-close>{{ 'common.close' | transloco }}</button>
      } @else {
        <button mat-button mat-dialog-close>{{ 'common.cancel' | transloco }}</button>
        <button mat-flat-button color="primary" [disabled]="busy || loading" (click)="setShared(true, showNames)">
          <mat-icon>ios_share</mat-icon> {{ 'recap.share' | transloco }}
        </button>
      }
    </mat-dialog-actions>
  `,
  styles: [`
    .share-lead { margin: 0 0 var(--opp-space-4); color: var(--opp-text-secondary); }
    .share-muted { margin: var(--opp-space-3) 0 0; color: var(--opp-text-muted); font-size: 0.875rem; }
    .share-label { display: block; font-weight: 600; margin-bottom: var(--opp-space-2); }
    .share-link { display: flex; gap: var(--opp-space-2); flex-wrap: wrap; }
    .share-input { flex: 1 1 14rem; min-width: 0; padding: 0.55rem 0.7rem; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-sm);
      background: var(--opp-surface-alt, var(--opp-surface)); color: var(--opp-text); font: inherit; }
    .share-open { display: inline-block; margin-top: var(--opp-space-2); font-size: 0.875rem; }
    .share-option { display: flex; align-items: center; gap: var(--opp-space-2); margin-top: var(--opp-space-4); cursor: pointer; }
    .share-error { color: var(--opp-danger, #b3261e); margin: var(--opp-space-3) 0 0; }
  `],
})
export class RecapShareDialogComponent implements OnInit {
  private readonly data = inject<RecapShareDialogData>(MAT_DIALOG_DATA);
  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);

  sharing: RecapSharing | null = null;
  showNames = false;
  loading = true;
  busy = false;
  error = '';

  get link(): string {
    return this.sharing?.token ? recapUrl(this.sharing.token) : '';
  }

  async ngOnInit(): Promise<void> {
    try {
      const result = await firstValueFrom(this.apollo.query<{ recapSharing: RecapSharing | ApiError }>({
        query: RECAP_SHARING, variables: { gameId: this.data.gameId }, fetchPolicy: 'network-only',
      }));
      const value = result.data?.recapSharing as (Partial<RecapSharing> & Partial<ApiError>) | undefined;
      if (value?.shared !== undefined) this.sharing = { shared: value.shared, token: value.token ?? null, showNames: !!value.showNames };
      else if (value?.message) this.error = value.message;
    } catch {
      this.error = this.i18n.translate('recap.failed');
    }
    this.loading = false;
    this.cdr.markForCheck();
  }

  async setShared(shared: boolean, showNames: boolean): Promise<void> {
    this.busy = true;
    this.error = '';
    this.cdr.markForCheck();
    try {
      const result = await firstValueFrom(this.apollo.mutate<{ setRecapSharing: { recapSharing: RecapSharing | null; errors: ApiError[] | null } }>({
        mutation: SET_RECAP_SHARING, variables: { input: { gameId: this.data.gameId, shared, showNames } },
      }));
      const payload = result.data?.setRecapSharing;
      if (payload?.errors?.length) this.error = payload.errors[0].message;
      else if (payload?.recapSharing) {
        const wasShared = !!this.sharing?.shared;
        this.sharing = { ...payload.recapSharing };
        if (shared && !wasShared) trackEvent('recap_shared', { names: showNames ? 1 : 0 });
      }
    } catch {
      this.error = this.i18n.translate('recap.failed');
    }
    this.busy = false;
    this.cdr.markForCheck();
  }

  select(event: FocusEvent): void {
    (event.target as HTMLInputElement).select();
  }

  copy(): void {
    navigator.clipboard.writeText(this.link)
      .then(() => this.snackBar.open(this.i18n.translate('recap.copied'), this.i18n.translate('common.close'), { duration: OPP_SNACKBAR_DURATION_DEFAULT }))
      .catch(() => this.snackBar.open(this.i18n.translate('room.copyFailed'), this.i18n.translate('common.close'), { duration: OPP_SNACKBAR_DURATION_DEFAULT }));
  }
}
