import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Apollo, gql } from 'apollo-angular';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OPP_SNACKBAR_DURATION_DEFAULT } from '../../../shared/constants';
import { Sprint, TeamTicket } from './team.graphql';

export const TEAM_ACCURACY = gql`
  query TeamAccuracy($teamId: UUID!) {
    teamAccuracy(teamId: $teamId) {
      ... on TeamAccuracyInfo {
        measured pending
        cards { value count medianDays minDays maxDays }
        outliers { days cardMedianDays ticket { ticketId gameId name externalKey externalUrl estimate sprintName } }
      }
      ... on ApplicationError { code message }
    }
  }`;

export const SET_TEAM_SUMMARY_WEBHOOK = gql`
  mutation SetTeamSummaryWebhook($input: SetTeamSummaryWebhookInput!) {
    setTeamSummaryWebhook(input: $input) { boolean errors { ... on ApplicationError { code message } } }
  }`;

export interface SprintStats {
  revealed: number;
  firstRoundConsensus: number;
  firstRoundConsensusShare: number | null;
  reVotesPerTicket: number | null;
  averageDiscussionSeconds: number | null;
  needsInfo: number;
}

interface CardAccuracy { value: string; count: number; medianDays: number; minDays: number; maxDays: number }
interface Accuracy {
  measured: number;
  pending: number;
  cards: CardAccuracy[];
  outliers: { days: number; cardMedianDays: number; ticket: TeamTicket }[];
}

/** "m:ss" for a number of seconds. */
export function clock(seconds: number | null | undefined): string {
  if (seconds === null || seconds === undefined) return '–';
  const s = Math.round(seconds);
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
}

/**
 * Team room insights (OPP-134, OPP-135, OPP-136): how the sessions went, how long each card really took in Jira,
 * and the webhook that gets the session summary. Team numbers only, never per player.
 */
@Component({
  selector: 'app-team-insights',
  standalone: true,
  imports: [TranslocoPipe, FormsModule, RouterLink, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="team-card" aria-labelledby="sessions-title">
      <h2 class="team-section-title" id="sessions-title">{{ 'insights.sessionsTitle' | transloco }}</h2>
      @if (!withStats.length) {
        <p class="muted">{{ 'insights.sessionsEmpty' | transloco }}</p>
      } @else {
        <div class="table-wrap">
          <table>
            <caption class="sr">{{ 'insights.sessionsTitle' | transloco }}</caption>
            <thead>
              <tr>
                <th scope="col">{{ 'insights.sprint' | transloco }}</th>
                <th scope="col">{{ 'insights.firstVote' | transloco }}</th>
                <th scope="col">{{ 'insights.reVotes' | transloco }}</th>
                <th scope="col">{{ 'insights.discussion' | transloco }}</th>
                <th scope="col">{{ 'insights.needsInfo' | transloco }}</th>
              </tr>
            </thead>
            <tbody>
              @for (s of withStats; track s.gameId) {
                <tr>
                  <th scope="row"><a class="link" [routerLink]="['/game', s.gameId]">{{ s.name }}</a></th>
                  <td>
                    <span class="bar" aria-hidden="true"><span [style.width.%]="(s.stats!.firstRoundConsensusShare ?? 0) * 100"></span></span>
                    <span class="opp-tabular">{{ percent(s.stats!.firstRoundConsensusShare) }}</span>
                  </td>
                  <td class="opp-tabular">{{ s.stats!.reVotesPerTicket ?? '–' }}</td>
                  <td class="opp-tabular">{{ clock(s.stats!.averageDiscussionSeconds) }}</td>
                  <td class="opp-tabular">{{ s.stats!.needsInfo }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <p class="muted">{{ 'insights.sessionsHint' | transloco }}</p>
      }
    </section>

    <section class="team-card" aria-labelledby="accuracy-title">
      <h2 class="team-section-title" id="accuracy-title">{{ 'insights.accuracyTitle' | transloco }}</h2>
      <p class="muted">{{ 'insights.accuracyLead' | transloco }}</p>
      @if (!accuracy) {
        <button mat-stroked-button type="button" (click)="loadAccuracy()" [disabled]="loading">{{ 'insights.loadAccuracy' | transloco }}</button>
      }
      @if (error) { <p class="error" role="alert">{{ error }}</p> }
      @if (accuracy) {
        @if (!accuracy.cards.length) {
          <p class="muted">{{ 'insights.accuracyEmpty' | transloco }}</p>
        } @else {
          <div class="table-wrap">
            <table>
              <caption class="sr">{{ 'insights.accuracyTitle' | transloco }}</caption>
              <thead>
                <tr>
                  <th scope="col">{{ 'insights.card' | transloco }}</th>
                  <th scope="col">{{ 'insights.tickets' | transloco }}</th>
                  <th scope="col">{{ 'insights.median' | transloco }}</th>
                  <th scope="col">{{ 'insights.range' | transloco }}</th>
                </tr>
              </thead>
              <tbody>
                @for (c of accuracy.cards; track c.value) {
                  <tr>
                    <th scope="row" class="opp-tabular">{{ c.value }}</th>
                    <td class="opp-tabular">{{ c.count }}</td>
                    <td class="opp-tabular">{{ 'insights.days' | transloco: { count: c.medianDays } }}</td>
                    <td>
                      <span class="range" aria-hidden="true">
                        <span class="range-span" [style.left.%]="pos(c.minDays)" [style.width.%]="pos(c.maxDays) - pos(c.minDays)"></span>
                        <span class="range-median" [style.left.%]="pos(c.medianDays)"></span>
                      </span>
                      <span class="opp-tabular muted">{{ c.minDays }}–{{ c.maxDays }}</span>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (accuracy.outliers.length) {
            <h3 class="sub">{{ 'insights.outliers' | transloco }}</h3>
            <ul class="outliers">
              @for (o of accuracy.outliers; track o.ticket.ticketId) {
                <li>
                  <b class="opp-tabular">{{ o.ticket.estimate }}</b>
                  @if (o.ticket.externalUrl) { <a class="link" [href]="o.ticket.externalUrl" target="_blank" rel="noopener">{{ o.ticket.externalKey }}</a> }
                  {{ o.ticket.name }}
                  <span class="muted">· {{ 'insights.outlierDays' | transloco: { days: o.days, median: o.cardMedianDays } }}</span>
                </li>
              }
            </ul>
          }
        }
        @if (accuracy.pending) {
          <p class="muted">{{ 'insights.pending' | transloco: { count: accuracy.pending } }}
            <button mat-button type="button" (click)="loadAccuracy()" [disabled]="loading">{{ 'insights.loadMore' | transloco }}</button></p>
        }
      }
    </section>

    @if (isOwner) {
      <section class="team-card" aria-labelledby="webhook-title">
        <h2 class="team-section-title" id="webhook-title">{{ 'insights.webhookTitle' | transloco }}</h2>
        <p class="muted">{{ 'insights.webhookLead' | transloco }}</p>
        <form class="webhook" (ngSubmit)="saveWebhook()">
          <input name="webhook" type="url" [(ngModel)]="webhookDraft" [attr.aria-label]="'insights.webhookLabel' | transloco"
            placeholder="https://hooks.slack.com/services/…" autocomplete="off">
          <button mat-stroked-button type="submit">{{ 'insights.save' | transloco }}</button>
        </form>
      </section>
    }
  `,
  styles: [`
    :host { display: contents; }
    .team-card { display: grid; gap: var(--opp-space-3); align-content: start; padding: var(--opp-space-5); border: 1px solid var(--opp-border);
      border-radius: var(--opp-radius-lg); background: var(--opp-surface); min-width: 0; }
    .team-section-title { margin: 0; font-size: var(--opp-font-size-lg); }
    .sub { margin: var(--opp-space-2) 0 0; font-size: var(--opp-font-size-md, 1rem); }
    .muted { color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); margin: 0; }
    .error { color: var(--opp-error, #c62828); margin: 0; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; font-size: var(--opp-font-size-sm); }
    th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid var(--opp-border); white-space: nowrap; }
    thead th { color: var(--opp-text-secondary); font-weight: 600; }
    tbody th { font-weight: 600; color: var(--opp-text); }
    .bar { display: inline-block; width: 64px; height: 8px; margin-right: 6px; border-radius: 4px; background: var(--opp-primary-50); overflow: hidden; vertical-align: middle; }
    .bar span { display: block; height: 100%; background: var(--opp-primary); border-radius: 4px; }
    .range { position: relative; display: inline-block; width: 120px; height: 10px; margin-right: 6px; vertical-align: middle; background: var(--opp-surface-hover); border-radius: 5px; }
    .range-span { position: absolute; top: 2px; height: 6px; border-radius: 3px; background: var(--opp-primary-300, var(--opp-primary)); min-width: 2px; }
    .range-median { position: absolute; top: -2px; width: 2px; height: 14px; margin-left: -1px; background: var(--opp-text); }
    .outliers { list-style: none; margin: 0; padding: 0; display: grid; gap: 4px; font-size: var(--opp-font-size-sm); }
    .outliers li { overflow-wrap: anywhere; }
    .webhook { display: flex; gap: var(--opp-space-2); flex-wrap: wrap; }
    .webhook input { flex: 1; min-width: 0; padding: 8px 10px; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md);
      background: var(--opp-surface); color: var(--opp-text); font: inherit; }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
  `],
})
export class TeamInsightsComponent implements OnChanges {
  @Input() teamId = '';
  @Input() sprints: (Sprint & { stats?: SprintStats | null })[] = [];
  @Input() isOwner = false;
  @Input() webhook: string | null = null;

  withStats: (Sprint & { stats?: SprintStats | null })[] = [];
  accuracy: Accuracy | null = null;
  loading = false;
  error = '';
  webhookDraft = '';
  readonly clock = clock;
  private maxDays = 1;

  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['sprints']) this.withStats = this.sprints.filter(s => !!s.stats?.revealed);
    if (changes['webhook']) this.webhookDraft = this.webhook ?? '';
  }

  percent(share: number | null): string {
    return share === null ? '–' : `${Math.round(share * 100)}%`;
  }

  pos(days: number): number {
    return Math.min(100, (days / this.maxDays) * 100);
  }

  loadAccuracy(): void {
    this.loading = true;
    this.error = '';
    this.apollo.query<{ teamAccuracy: Accuracy & { code?: string; message?: string } }>({ query: TEAM_ACCURACY, variables: { teamId: this.teamId } })
      .subscribe({
        next: ({ data }) => {
          this.loading = false;
          const result = data?.teamAccuracy;
          if (!result || result.code) {
            this.error = result?.message ?? this.i18n.translate('insights.accuracyFailed');
          } else {
            this.accuracy = result;
            this.maxDays = Math.max(1, ...result.cards.map(c => c.maxDays));
          }
          this.cdr.markForCheck();
        },
        error: () => { this.loading = false; this.error = this.i18n.translate('insights.accuracyFailed'); this.cdr.markForCheck(); },
      });
  }

  saveWebhook(): void {
    this.apollo.mutate<{ setTeamSummaryWebhook: { errors?: { message: string }[] } }>({
      mutation: SET_TEAM_SUMMARY_WEBHOOK, variables: { input: { teamId: this.teamId, url: this.webhookDraft.trim() || null } },
    }).subscribe({
      next: ({ data }) => {
        const errors = data?.setTeamSummaryWebhook?.errors;
        this.notify(errors?.length ? errors[0].message : this.i18n.translate('insights.webhookSaved'));
      },
      error: () => this.notify(this.i18n.translate('common.tryAgainNetwork')),
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, this.i18n.translate('common.close'), { duration: OPP_SNACKBAR_DURATION_DEFAULT * 2 });
  }
}
