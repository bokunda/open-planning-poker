import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Apollo } from 'apollo-angular';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { DISCONNECT_TRACKER, IMPORT_TRACKER_ISSUES, TRACKER_AUTHORIZE_URL, TRACKER_ISSUES, TRACKER_SOURCES, TrackerInfo, TrackerIssue } from '../gql/trackers.graphql';

type Err = { code: string; message: string };

/**
 * "From GitHub / Linear / Azure DevOps" in the import dialog (OPP-130): connect once, pick a repository, team or
 * project, optionally filter, preview and import. Imported tickets keep their key and link for "Save to KEY".
 */
@Component({
  selector: 'app-tracker-import',
  standalone: true,
  imports: [TranslocoPipe, FormsModule, MatButtonModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tracker">
      @if (!tracker.connected) {
        <p>{{ (tracker.needsReconnect ? 'trackers.reconnectLead' : 'trackers.connectLead') | transloco: { name: tracker.name } }}</p>
        <button mat-flat-button color="primary" type="button" (click)="connect()" [disabled]="busy">{{ 'trackers.connect' | transloco: { name: tracker.name } }}</button>
      } @else {
        <p class="muted">{{ 'trackers.connectedAs' | transloco: { name: tracker.accountName ?? tracker.name } }}
          <button mat-button type="button" (click)="disconnect()">{{ 'trackers.disconnect' | transloco }}</button></p>
        <label class="field">{{ 'trackers.source' | transloco }}
          <select [(ngModel)]="sourceId" (change)="issues = null" [disabled]="!sources.length">
            @for (s of sources; track s.id) { <option [value]="s.id">{{ s.name }}</option> }
          </select>
        </label>
        <label class="field">{{ 'trackers.query' | transloco }}
          <input [(ngModel)]="query" [placeholder]="tracker.queryHint" autocomplete="off">
        </label>
        <button mat-stroked-button type="button" (click)="preview()" [disabled]="!sourceId || busy">{{ 'trackers.preview' | transloco }}</button>
        @if (busy) { <mat-spinner diameter="24"></mat-spinner> }
        @if (issues) {
          @if (!issues.length) { <p class="muted">{{ 'trackers.none' | transloco }}</p> }
          <ul class="issues">
            @for (i of issues; track i.key) {
              <li>
                <label>
                  <input type="checkbox" [checked]="selected.has(i.key)" [disabled]="i.alreadyInGame" (change)="toggle(i.key)">
                  <b>{{ i.key }}</b> {{ i.title }}
                  @if (i.estimate !== null) { <span class="muted"> · {{ i.estimate }}</span> }
                  @if (i.alreadyInGame) { <span class="muted"> · {{ 'trackers.inGame' | transloco }}</span> }
                </label>
              </li>
            }
          </ul>
          <button mat-flat-button color="primary" type="button" (click)="import()" [disabled]="!selected.size || busy">
            {{ 'trackers.import' | transloco: { count: selected.size } }}
          </button>
        }
      }
      @if (error) { <p class="error" role="alert">{{ error }}</p> }
    </div>
  `,
  styles: [`
    .tracker { display: grid; gap: var(--opp-space-3); padding: var(--opp-space-3) 0; }
    .muted { color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); margin: 0; }
    .field { display: grid; gap: 4px; font-size: var(--opp-font-size-sm); font-weight: 600; }
    .field select, .field input { padding: 8px 10px; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md);
      background: var(--opp-surface); color: var(--opp-text); font: inherit; font-weight: 400; }
    .issues { list-style: none; margin: 0; padding: 0; display: grid; gap: 4px; max-height: 260px; overflow: auto; }
    .issues label { display: flex; gap: 8px; align-items: baseline; font-size: var(--opp-font-size-sm); overflow-wrap: anywhere; }
    .error { color: var(--opp-error, #c62828); margin: 0; }
  `],
})
export class TrackerImportComponent implements OnInit {
  @Input({ required: true }) tracker!: TrackerInfo;
  @Input({ required: true }) gameId!: string;
  @Output() imported = new EventEmitter<number>();

  sources: { id: string; name: string }[] = [];
  sourceId = '';
  query = '';
  issues: TrackerIssue[] | null = null;
  selected = new Set<string>();
  busy = false;
  error = '';

  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly i18n = inject(TranslocoService);

  ngOnInit(): void {
    if (this.tracker.connected) this.loadSources();
  }

  connect(): void {
    this.busy = true;
    const returnPath = window.location.pathname;
    this.apollo.query<{ trackerAuthorizeUrl: { url?: string } & Partial<Err> }>({
      query: TRACKER_AUTHORIZE_URL, variables: { provider: this.tracker.provider, returnPath },
    }).subscribe({
      next: ({ data }) => {
        const url = data?.trackerAuthorizeUrl?.url;
        if (url) { window.location.href = url; return; }
        this.fail(data?.trackerAuthorizeUrl?.message);
      },
      error: () => this.fail(),
    });
  }

  disconnect(): void {
    this.apollo.mutate({ mutation: DISCONNECT_TRACKER, variables: { input: { provider: this.tracker.provider } } }).subscribe(() => {
      this.tracker = { ...this.tracker, connected: false, accountName: null };
      this.sources = [];
      this.issues = null;
      this.cdr.markForCheck();
    });
  }

  toggle(key: string): void {
    const next = new Set(this.selected);
    if (next.has(key)) next.delete(key); else next.add(key);
    this.selected = next;
  }

  preview(): void {
    this.busy = true;
    this.error = '';
    this.apollo.query<{ trackerIssues: { issues?: TrackerIssue[] } & Partial<Err> }>({
      query: TRACKER_ISSUES,
      variables: { provider: this.tracker.provider, gameId: this.gameId, sourceId: this.sourceId, query: this.query.trim() || null },
    }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        const issues = data?.trackerIssues?.issues;
        if (!issues) { this.fail(data?.trackerIssues?.message); return; }
        this.issues = issues;
        this.selected = new Set(issues.filter(i => !i.alreadyInGame).map(i => i.key));
        this.cdr.markForCheck();
      },
      error: () => this.fail(),
    });
  }

  import(): void {
    this.busy = true;
    this.apollo.mutate<{ importTrackerIssues: { trackerImportSummary?: { imported: number }; errors?: Err[] } }>({
      mutation: IMPORT_TRACKER_ISSUES,
      variables: { input: { provider: this.tracker.provider, gameId: this.gameId, sourceId: this.sourceId, query: this.query.trim() || null, keys: [...this.selected] } },
    }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        const result = data?.importTrackerIssues;
        if (result?.errors?.length || !result?.trackerImportSummary) { this.fail(result?.errors?.[0]?.message); return; }
        this.imported.emit(result.trackerImportSummary.imported);
      },
      error: () => this.fail(),
    });
  }

  private loadSources(): void {
    this.apollo.query<{ trackerSources: { items?: { id: string; name: string }[] } & Partial<Err> }>({
      query: TRACKER_SOURCES, variables: { provider: this.tracker.provider },
    }).subscribe({
      next: ({ data }) => {
        const items = data?.trackerSources?.items;
        if (!items) { this.fail(data?.trackerSources?.message); return; }
        this.sources = items;
        this.sourceId = items[0]?.id ?? '';
        this.cdr.markForCheck();
      },
      error: () => this.fail(),
    });
  }

  private fail(message?: string): void {
    this.busy = false;
    this.error = message ?? this.i18n.translate('trackers.failed');
    this.cdr.markForCheck();
  }
}
