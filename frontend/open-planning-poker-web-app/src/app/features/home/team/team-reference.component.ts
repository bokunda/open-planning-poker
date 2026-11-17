import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Apollo } from 'apollo-angular';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OppDatePipe } from '../../../i18n/opp-date.pipe';
import { OPP_SNACKBAR_DURATION_DEFAULT } from '../../../shared/constants';
import { PIN_TEAM_ANCHOR, TEAM_ANCHORS, TEAM_HISTORY, TeamAnchor, TeamTicket, UNPIN_TEAM_ANCHOR } from './team.graphql';

const PAGE = 20;

/** Numeric cards first, in order; others after them. */
export function byCard(a: string, b: string): number {
  const na = Number(a), nb = Number(b);
  const fa = Number.isFinite(na), fb = Number.isFinite(nb);
  if (fa && fb) return na - nb;
  if (fa !== fb) return fa ? -1 : 1;
  return a.localeCompare(b);
}

/**
 * Team room (OPP-127, OPP-129): the team's reference tickets by card value, and a search over every estimated ticket
 * of its sprints. Any estimated ticket can be pinned as the reference for its card (two per card).
 */
@Component({
  selector: 'app-team-reference',
  standalone: true,
  imports: [TranslocoPipe, OppDatePipe, FormsModule, RouterLink, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="team-card" aria-labelledby="anchors-title">
      <h2 class="team-section-title" id="anchors-title">{{ 'reference.anchorsTitle' | transloco }}</h2>
      @if (!anchorGroups.length) {
        <p class="muted">{{ 'reference.anchorsEmpty' | transloco }}</p>
      }
      <dl class="anchors">
        @for (group of anchorGroups; track group.value) {
          <div class="anchor-group">
            <dt class="card opp-tabular">{{ group.value }}</dt>
            @for (a of group.items; track a.id) {
              <dd class="anchor">
                <span class="anchor-name">
                  @if (a.ticket.externalUrl) { <a class="link" [href]="a.ticket.externalUrl" target="_blank" rel="noopener">{{ a.ticket.externalKey }}</a> }
                  {{ a.ticket.name }}
                </span>
                @if (a.note) { <span class="muted">{{ a.note }}</span> }
                <button mat-button type="button" class="small" (click)="unpin(a)">{{ 'reference.unpin' | transloco }}</button>
              </dd>
            }
          </div>
        }
      </dl>
    </section>

    <section class="team-card" aria-labelledby="search-title">
      <h2 class="team-section-title" id="search-title">{{ 'reference.searchTitle' | transloco }}</h2>
      <form class="search" role="search" (ngSubmit)="search(0)">
        <input name="q" [(ngModel)]="query" [attr.aria-label]="'reference.searchLabel' | transloco"
          [placeholder]="'reference.searchPlaceholder' | transloco" autocomplete="off">
        <select name="value" [(ngModel)]="value" (change)="search(0)" [attr.aria-label]="'reference.cardFilter' | transloco">
          <option value="">{{ 'reference.anyCard' | transloco }}</option>
          @for (card of cards; track card) { <option [value]="card">{{ card }}</option> }
        </select>
        <button mat-stroked-button type="submit">{{ 'reference.search' | transloco }}</button>
      </form>
      @if (results) {
        <p class="muted" role="status">{{ 'reference.found' | transloco: { count: total } }}</p>
        <ul class="results">
          @for (t of results; track t.ticketId) {
            <li class="result">
              <b class="card opp-tabular">{{ t.estimate }}</b>
              <span class="result-name">
                @if (t.externalUrl) { <a class="link" [href]="t.externalUrl" target="_blank" rel="noopener">{{ t.externalKey }}</a> }
                {{ t.name }}
                <span class="muted"> · <a class="link" [routerLink]="['/game', t.gameId]">{{ t.sprintName }}</a> · {{ t.sprintDate | oppDate:'d MMM y' }}</span>
              </span>
              @if (isPinned(t)) {
                <span class="muted">{{ 'reference.pinned' | transloco }}</span>
              } @else if (pinning === t.ticketId) {
                <form class="pin-form" (ngSubmit)="pin(t)">
                  <input name="note" [(ngModel)]="note" maxlength="200" [attr.aria-label]="'reference.noteLabel' | transloco"
                    [placeholder]="'reference.notePlaceholder' | transloco" autocomplete="off">
                  <button mat-flat-button color="primary" type="submit">{{ 'reference.pin' | transloco }}</button>
                </form>
              } @else {
                <button mat-button type="button" class="small" (click)="pinning = t.ticketId; note = ''">{{ 'reference.pinAs' | transloco: { value: t.estimate } }}</button>
              }
            </li>
          }
        </ul>
        @if (total > results.length + skip || skip > 0) {
          <div class="pager">
            <button mat-button type="button" [disabled]="skip === 0" (click)="search(skip - page)">{{ 'reference.previous' | transloco }}</button>
            <button mat-button type="button" [disabled]="skip + page >= total" (click)="search(skip + page)">{{ 'reference.next' | transloco }}</button>
          </div>
        }
      }
    </section>
  `,
  styles: [`
    :host { display: contents; }
    .team-card { display: grid; gap: var(--opp-space-3); align-content: start; padding: var(--opp-space-5); border: 1px solid var(--opp-border);
      border-radius: var(--opp-radius-lg); background: var(--opp-surface); min-width: 0; }
    .team-section-title { display: flex; align-items: center; gap: var(--opp-space-2); margin: 0; font-size: var(--opp-font-size-lg); }
    .muted { color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); }
    .anchors { display: grid; gap: var(--opp-space-3); margin: 0; }
    .anchor-group { display: grid; grid-template-columns: 48px minmax(0, 1fr); gap: var(--opp-space-1) var(--opp-space-3); align-items: start; }
    .anchor-group dd { grid-column: 2; margin: 0; }
    .card { display: inline-grid; place-items: center; min-width: 40px; height: 40px; padding: 0 6px; border-radius: var(--opp-radius-md);
      background: var(--opp-primary-50); color: var(--opp-primary); font-family: var(--opp-font-display); font-weight: 800; }
    .anchor { display: flex; flex-wrap: wrap; align-items: center; gap: var(--opp-space-1) var(--opp-space-3); }
    .anchor-name, .result-name { color: var(--opp-text); overflow-wrap: anywhere; }
    .search { display: flex; flex-wrap: wrap; gap: var(--opp-space-2); margin-bottom: var(--opp-space-3); }
    .search input, .search select, .pin-form input { flex: 1; min-width: 0; padding: 8px 10px; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md);
      background: var(--opp-surface); color: var(--opp-text); font: inherit; }
    .search select { flex: 0 0 auto; }
    .results { list-style: none; margin: 0; padding: 0; display: grid; gap: var(--opp-space-2); }
    .result { display: grid; grid-template-columns: 48px minmax(0, 1fr) auto; gap: var(--opp-space-3); align-items: center; }
    .pin-form { display: flex; gap: var(--opp-space-2); grid-column: 2 / -1; }
    .pager { display: flex; justify-content: space-between; margin-top: var(--opp-space-2); }
    @media (max-width: 520px) { .result { grid-template-columns: 40px minmax(0, 1fr); } .result > :last-child { grid-column: 2; justify-self: start; } }
  `],
})
export class TeamReferenceComponent implements OnChanges {
  @Input() teamId = '';

  anchors: TeamAnchor[] = [];
  anchorGroups: { value: string; items: TeamAnchor[] }[] = [];
  results: TeamTicket[] | null = null;
  cards: string[] = [];
  total = 0;
  skip = 0;
  readonly page = PAGE;
  query = '';
  value = '';
  note = '';
  pinning: string | null = null;

  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['teamId'] && this.teamId) {
      this.loadAnchors();
      this.search(0);
    }
  }

  isPinned(t: TeamTicket): boolean {
    return this.anchors.some(a => a.ticket.ticketId === t.ticketId);
  }

  search(skip: number): void {
    this.skip = Math.max(0, skip);
    this.apollo.query<{ team: { history: { totalCount: number; items: TeamTicket[] } } }>({
      query: TEAM_HISTORY,
      variables: { id: this.teamId, search: this.query.trim() || null, value: this.value || null, skip: this.skip, take: PAGE },
    }).subscribe(({ data }) => {
      const history = data?.team?.history;
      this.results = history?.items ?? [];
      this.total = history?.totalCount ?? 0;
      if (!this.query && !this.value && !this.skip) {
        this.cards = [...new Set(this.results.map(r => r.estimate))].sort(byCard);
      }
      this.cdr.markForCheck();
    });
  }

  pin(t: TeamTicket): void {
    this.apollo.mutate<{ pinTeamAnchor: { errors?: { message: string }[] } }>({
      mutation: PIN_TEAM_ANCHOR, variables: { input: { teamId: this.teamId, ticketId: t.ticketId, note: this.note.trim() || null } },
    }).subscribe({
      next: ({ data }) => {
        const errors = data?.pinTeamAnchor?.errors;
        if (errors?.length) { this.notify(errors[0].message); return; }
        this.pinning = null;
        this.loadAnchors();
      },
      error: () => this.notify(this.i18n.translate('common.tryAgainNetwork')),
    });
  }

  unpin(a: TeamAnchor): void {
    this.apollo.mutate({ mutation: UNPIN_TEAM_ANCHOR, variables: { input: { teamId: this.teamId, anchorId: a.id } } })
      .subscribe({ next: () => this.loadAnchors(), error: () => this.notify(this.i18n.translate('common.tryAgainNetwork')) });
  }

  private loadAnchors(): void {
    this.apollo.query<{ team: { anchors: TeamAnchor[] } }>({ query: TEAM_ANCHORS, variables: { id: this.teamId } })
      .subscribe(({ data }) => {
        this.anchors = data?.team?.anchors ?? [];
        const values = [...new Set(this.anchors.map(a => a.value))].sort(byCard);
        this.anchorGroups = values.map(value => ({ value, items: this.anchors.filter(a => a.value === value) }));
        this.cdr.markForCheck();
      });
  }

  private notify(message: string): void {
    this.snackBar.open(message, this.i18n.translate('common.close'), { duration: OPP_SNACKBAR_DURATION_DEFAULT * 2 });
  }
}
