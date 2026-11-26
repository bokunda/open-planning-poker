import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Apollo, gql } from 'apollo-angular';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OppDatePipe } from '../../i18n/opp-date.pipe';
import { ActivityEntry, AuditScope, EMPTY_ID, actionIcon, actionKey, appendPage, nextCursor, providerName } from './activity-helpers';

export const ACTIVITY_LOG = gql`
  query ActivityLog($scope: AuditScope!, $scopeId: UUID!, $before: DateTime, $take: Int!) {
    activityLog(scope: $scope, scopeId: $scopeId, before: $before, take: $take) {
      ... on ActivityLog { hasMore entries { id action type actorId actorName targetId targetName ticketId detail value createdOn } }
      ... on ApplicationError { code message }
    }
  }`;

const PAGE = 30;

type Result = { entries?: ActivityEntry[]; hasMore?: boolean; code?: string; message?: string } | null | undefined;

/**
 * The activity log (audit log, GitHub #20) of a game, team or organization: who changed what, newest first.
 * `card` is a section for the team and organization pages; `panel` is a collapsible box for the game sidebar
 * that loads only while it is open. Raise `refresh` to load the first page again.
 */
@Component({
  selector: 'app-activity-log',
  standalone: true,
  imports: [NgTemplateOutlet, TranslocoPipe, OppDatePipe, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class.panel]': "variant === 'panel'", '[class.collapsed]': "variant === 'panel' && collapsed" },
  template: `
    @if (variant === 'panel') {
      <div class="panel-box" [class.panel-box--collapsed]="collapsed" data-testid="activity-panel">
        <div class="panel-header" (click)="toggle()">
          <span id="activity-title-{{ scopeId }}">{{ 'activity.title' | transloco }}</span>
          <button mat-icon-button type="button" (click)="$event.stopPropagation(); toggle()"
            [attr.aria-label]="(collapsed ? 'activity.show' : 'activity.hide') | transloco" [attr.aria-expanded]="!collapsed">
            <mat-icon>{{ collapsed ? 'expand_more' : 'expand_less' }}</mat-icon>
          </button>
        </div>
        @if (!collapsed) {
          <div class="panel-body" role="region" tabindex="0" [attr.aria-label]="'activity.title' | transloco">
            <ng-container *ngTemplateOutlet="body"></ng-container>
          </div>
        }
      </div>
    } @else {
      <section class="team-card" [attr.aria-labelledby]="'activity-title-' + scopeId" data-testid="activity-card">
        <div class="head">
          <h2 class="team-section-title" id="activity-title-{{ scopeId }}">{{ 'activity.title' | transloco }}</h2>
          <button mat-button type="button" (click)="reload()" [disabled]="loading">{{ 'activity.refresh' | transloco }}</button>
        </div>
        <p class="muted">{{ 'activity.lead' | transloco }}</p>
        <ng-container *ngTemplateOutlet="body"></ng-container>
      </section>
    }

    <ng-template #body>
      @if (error) {
        <p class="muted" role="alert">{{ error }}</p>
      } @else if (!entries.length && !loading) {
        <p class="muted">{{ 'activity.empty' | transloco }}</p>
      } @else {
        <ol class="entries" [attr.aria-label]="'activity.title' | transloco" aria-live="polite">
          @for (e of entries; track e.id) {
            <li class="entry" data-testid="activity-entry" [attr.data-action]="e.action">
              <mat-icon [class]="'icon icon--' + e.type.toLowerCase()" aria-hidden="true">{{ icon(e) }}</mat-icon>
              <span class="text">{{ key(e) | transloco: params(e) }}</span>
              <time class="when" [attr.datetime]="e.createdOn">{{ e.createdOn | oppDate: 'd MMM, HH:mm' }}</time>
            </li>
          }
        </ol>
      }
      <div class="row">
        @if (hasMore) {
          <button mat-stroked-button type="button" (click)="more()" [disabled]="loading">{{ 'activity.more' | transloco }}</button>
        }
        @if (variant === 'panel') {
          <button mat-button type="button" (click)="reload()" [disabled]="loading">{{ 'activity.refresh' | transloco }}</button>
        }
      </div>
    </ng-template>
  `,
  styles: [`
    :host { display: contents; }
    :host(.panel) { display: flex; flex-direction: column; flex: 0 1 auto; min-height: 0; }
    .team-card { display: grid; gap: var(--opp-space-3); align-content: start; padding: var(--opp-space-5); border: 1px solid var(--opp-border);
      border-radius: var(--opp-radius-lg); background: var(--opp-surface); min-width: 0; }
    .head { display: flex; align-items: center; justify-content: space-between; gap: var(--opp-space-2); }
    .team-section-title { margin: 0; font-size: var(--opp-font-size-lg); }
    .muted { color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); margin: 0; }
    .row { display: flex; flex-wrap: wrap; gap: var(--opp-space-2); }
    .row:empty { display: none; }
    .entries { list-style: none; margin: 0; padding: 0; display: grid; gap: 2px; }
    .entry { display: grid; grid-template-columns: auto 1fr auto; align-items: start; gap: var(--opp-space-2);
      padding: var(--opp-space-2) 0; border-bottom: 1px solid var(--opp-border); font-size: var(--opp-font-size-sm); }
    .entry:last-child { border-bottom: 0; }
    .text { overflow-wrap: anywhere; }
    .when { color: var(--opp-text-secondary); white-space: nowrap; font-variant-numeric: tabular-nums; }
    .icon { font-size: 18px; width: 18px; height: 18px; color: var(--opp-text-secondary); }
    .icon--create { color: var(--opp-success, #2e7d32); }
    .icon--delete { color: var(--opp-error, #c62828); }
    .panel-box { display: flex; flex-direction: column; min-height: 0; background: var(--opp-surface); border-radius: var(--opp-radius-md);
      box-shadow: var(--opp-shadow-sm); overflow: hidden; border: 1px solid var(--opp-border); }
    .panel-header { display: flex; justify-content: space-between; align-items: center; padding: var(--opp-space-2) var(--opp-space-3);
      background: var(--opp-primary); color: var(--opp-text-on-primary); font-weight: 600; font-size: var(--opp-font-size-sm);
      cursor: pointer; user-select: none; }
    .panel-header button { color: var(--opp-text-on-primary); }
    .panel-body { display: grid; gap: var(--opp-space-2); padding: var(--opp-space-2) var(--opp-space-3); max-height: 320px; overflow-y: auto; }
  `],
})
export class ActivityLogComponent implements OnChanges {
  @Input() scope: AuditScope = 'GAME';
  @Input() scopeId = '';
  @Input() variant: 'card' | 'panel' = 'card';
  /** Panel only: starts closed, so the room does not load the log for nothing. */
  @Input() collapsed = true;
  @Output() collapsedChange = new EventEmitter<boolean>();
  /** Raise to reload the first page (e.g. when the room changed); ignored while the panel is closed. */
  @Input() refresh = 0;

  private readonly apollo = inject(Apollo);
  private readonly i18n = inject(TranslocoService);
  private readonly cdr = inject(ChangeDetectorRef);

  entries: ActivityEntry[] = [];
  hasMore = false;
  loading = false;
  error: string | null = null;
  private loaded = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['scopeId'] || changes['scope']) {
      this.entries = [];
      this.loaded = false;
    }
    if (this.isOpen && this.scopeId && (!this.loaded || changes['refresh'])) this.reload();
  }

  private get isOpen(): boolean {
    return this.variant === 'card' || !this.collapsed;
  }

  toggle(): void {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
    if (!this.collapsed) this.reload();
  }

  reload(): void {
    this.fetch(null);
  }

  more(): void {
    this.fetch(nextCursor(this.entries));
  }

  key(entry: ActivityEntry): string {
    return actionKey(entry);
  }

  icon(entry: ActivityEntry): string {
    return actionIcon(entry.type);
  }

  params(entry: ActivityEntry): Record<string, string> {
    const actor = entry.actorId === EMPTY_ID ? this.t('activity.system') : entry.actorName || this.t('activity.someone');
    const value = entry.action === 'player.role' || entry.action === 'org.memberRole'
      ? this.t(`activity.roles.${(entry.value ?? '').toLowerCase()}`)
      : entry.action.startsWith('integration.') ? providerName(entry.value) : entry.value || '–';
    return {
      actor,
      target: entry.targetName ?? (entry.action.startsWith('org.team') ? entry.detail ?? '' : this.t('activity.someone')),
      detail: entry.detail ?? '',
      value,
    };
  }

  private t(key: string): string {
    return this.i18n.translate<string>(key) ?? '';
  }

  private fetch(before: string | null): void {
    if (!this.scopeId) return;
    this.loading = true;
    this.apollo.query<{ activityLog: Result }>({
      query: ACTIVITY_LOG,
      variables: { scope: this.scope, scopeId: this.scopeId, before, take: PAGE },
      fetchPolicy: 'no-cache',
    }).subscribe({
      next: ({ data }) => {
        const result = data?.activityLog;
        this.loading = false;
        this.loaded = true;
        if (!result || result.code) {
          this.error = result?.message ?? this.i18n.translate('activity.loadFailed');
        } else {
          this.error = null;
          const page = result.entries ?? [];
          this.entries = before ? appendPage(this.entries, page) : page;
          this.hasMore = !!result.hasMore;
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.error = this.i18n.translate('activity.loadFailed');
        this.cdr.markForCheck();
      },
    });
  }
}
