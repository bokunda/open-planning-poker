import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Apollo } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import { RoomTicket } from '../room-types';
import { SAVE_ESTIMATE_TO_JIRA } from '../gql/jira.graphql';
import { SAVE_ESTIMATE_TO_TRACKER, trackerOfUrl } from '../gql/trackers.graphql';

export interface SaveAllDialogData {
  tickets: RoomTicket[];
}

type Status = 'waiting' | 'saving' | 'saved' | 'failed';

interface Row {
  ticket: RoomTicket;
  tracker: string;
  status: Status;
  error?: string;
}

const TRACKER_NAMES: Record<string, string> = { jira: 'Jira', github: 'GitHub', linear: 'Linear', 'azure-devops': 'Azure DevOps' };

/**
 * Saves the final estimate of every linked ticket back to its tracker, one after another (OPP-145). Failed tickets
 * keep their reason and can be retried on their own.
 */
@Component({
  selector: 'app-save-all-dialog',
  standalone: true,
  imports: [TranslocoPipe, MatDialogModule, MatButtonModule, MatIconModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ 'saveAll.title' | transloco }}</h2>
    <mat-dialog-content>
      @if (!rows.length) {
        <p class="save-lead">{{ 'saveAll.none' | transloco }}</p>
      } @else {
        <p class="save-lead">{{ 'saveAll.lead' | transloco: { count: rows.length } }}
          @if (skipped) { {{ 'saveAll.skipped' | transloco: { count: skipped } }} }
        </p>
        @if (hasJira && !started) {
          <label class="save-option">
            <input type="checkbox" [checked]="addComment" (change)="addComment = !addComment">
            {{ 'saveAll.jiraComment' | transloco }}
          </label>
        }
        @if (started) {
          <mat-progress-bar mode="determinate" [value]="progress" [attr.aria-label]="'saveAll.progress' | transloco"></mat-progress-bar>
          <p class="save-summary" aria-live="polite">
            {{ 'saveAll.summary' | transloco: { saved: count('saved'), total: rows.length } }}
            @if (count('failed')) { · {{ 'saveAll.failedCount' | transloco: { count: count('failed') } }} }
          </p>
        }
        <ul class="save-list">
          @for (row of rows; track row.ticket.id) {
            <li class="save-row" [class.save-row--failed]="row.status === 'failed'">
              <span class="save-status" [attr.aria-label]="('saveAll.status.' + row.status) | transloco">
                @switch (row.status) {
                  @case ('saved') { <mat-icon svgIcon="opp-check" class="save-ok"></mat-icon> }
                  @case ('failed') { <span class="save-x">!</span> }
                  @case ('saving') { <span class="save-dot save-dot--busy"></span> }
                  @default { <span class="save-dot"></span> }
                }
              </span>
              <span class="save-main">
                <span class="save-name"><strong>{{ row.ticket.externalKey }}</strong> → {{ row.ticket.finalEstimate }}
                  <span class="save-tracker">{{ row.tracker }}</span></span>
                @if (row.error) { <span class="save-error">{{ row.error }}</span> }
              </span>
            </li>
          }
        </ul>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close [disabled]="running">{{ 'common.close' | transloco }}</button>
      @if (rows.length && !started) {
        <button mat-flat-button color="primary" (click)="run()" cdkFocusInitial>{{ 'saveAll.save' | transloco: { count: rows.length } }}</button>
      }
      @if (started && !running && count('failed')) {
        <button mat-flat-button color="primary" (click)="run(true)">{{ 'saveAll.retry' | transloco: { count: count('failed') } }}</button>
      }
    </mat-dialog-actions>
  `,
  styles: [`
    .save-lead { margin: 0 0 var(--opp-space-3); color: var(--opp-text-secondary); line-height: 1.5; }
    .save-option { display: flex; align-items: center; gap: var(--opp-space-2); margin-bottom: var(--opp-space-3); font-size: var(--opp-font-size-sm); }
    .save-summary { margin: var(--opp-space-2) 0; font-size: var(--opp-font-size-sm); }
    .save-list { list-style: none; margin: 0; padding: 0; display: grid; gap: 2px; }
    .save-row { display: flex; gap: var(--opp-space-2); align-items: flex-start; padding: 6px 0; border-top: 1px solid var(--opp-border-light); }
    .save-row:first-child { border-top: 0; }
    .save-status { flex: none; display: inline-grid; place-items: center; width: 20px; height: 20px; }
    .save-ok { width: 18px; height: 18px; color: var(--opp-success); }
    .save-x { display: inline-grid; place-items: center; width: 18px; height: 18px; border-radius: 50%; background: var(--opp-danger); color: #fff; font-size: 12px; font-weight: 800; }
    .save-dot { width: 8px; height: 8px; border-radius: 50%; background: var(--opp-border); }
    .save-dot--busy { background: var(--opp-primary); }
    .save-main { display: grid; gap: 2px; min-width: 0; }
    .save-name { font-size: var(--opp-font-size-sm); overflow-wrap: anywhere; }
    .save-tracker { margin-left: 6px; font-size: var(--opp-font-size-xs); color: var(--opp-text-muted); }
    .save-error { font-size: var(--opp-font-size-xs); color: var(--opp-danger); }
  `]
})
export class SaveAllDialogComponent {
  private readonly data = inject<SaveAllDialogData>(MAT_DIALOG_DATA);
  private readonly apollo = inject(Apollo);
  private readonly i18n = inject(TranslocoService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly rows: Row[];
  /** Linked tickets without a final estimate, or tickets without a link. */
  readonly skipped: number;
  addComment = false;
  started = false;
  running = false;

  constructor() {
    const ready = this.data.tickets.filter(t => !!t.externalKey && !!t.finalEstimate);
    this.rows = ready.map(ticket => {
      const tracker = trackerOfUrl(ticket.externalUrl) ?? 'jira';
      return { ticket, tracker: TRACKER_NAMES[tracker] ?? tracker, status: 'waiting' as Status };
    });
    this.skipped = this.data.tickets.length - ready.length;
  }

  get hasJira(): boolean {
    return this.rows.some(r => r.tracker === 'Jira');
  }

  get progress(): number {
    const done = this.rows.filter(r => r.status === 'saved' || r.status === 'failed').length;
    return this.rows.length ? Math.round(100 * done / this.rows.length) : 0;
  }

  count(status: Status): number {
    return this.rows.filter(r => r.status === status).length;
  }

  async run(failedOnly = false): Promise<void> {
    this.started = true;
    this.running = true;
    const todo = this.rows.filter(r => failedOnly ? r.status === 'failed' : r.status !== 'saved');
    todo.forEach(r => { r.status = 'waiting'; r.error = undefined; });
    this.cdr.markForCheck();
    // One after another: the trackers limit requests per user, and the order matches the list.
    for (const row of todo) {
      row.status = 'saving';
      this.cdr.markForCheck();
      row.error = await this.save(row.ticket);
      row.status = row.error ? 'failed' : 'saved';
      this.cdr.markForCheck();
    }
    this.running = false;
    this.cdr.markForCheck();
  }

  /** Returns the error message, or undefined when it was saved. */
  private async save(ticket: RoomTicket): Promise<string | undefined> {
    const tracker = trackerOfUrl(ticket.externalUrl);
    const isJira = !tracker || tracker === 'jira';
    const field = isJira ? 'saveEstimateToJira' : 'saveEstimateToTracker';
    try {
      const result = await firstValueFrom(this.apollo.mutate<Record<string, { errors?: { message: string }[] } | undefined>>({
        mutation: isJira ? SAVE_ESTIMATE_TO_JIRA : SAVE_ESTIMATE_TO_TRACKER,
        variables: { input: isJira ? { ticketId: ticket.id, addComment: this.addComment } : { ticketId: ticket.id } },
      }));
      const errors = result.data?.[field]?.errors;
      return errors?.length ? errors[0].message : undefined;
    } catch {
      return this.i18n.translate('common.tryAgainNetwork');
    }
  }
}
