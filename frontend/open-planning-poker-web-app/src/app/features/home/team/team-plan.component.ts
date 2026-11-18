import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Apollo, gql } from 'apollo-angular';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OPP_SNACKBAR_DURATION_DEFAULT } from '../../../shared/constants';
import { GET_JIRA_BOARDS, GET_JIRA_SPRINTS } from '../game/gql/jira.graphql';
import { trackerOfUrl } from '../game/gql/trackers.graphql';
import { Sprint } from './team.graphql';

const PLAN = `gameId sprintDays sprintDaysSet personDays plannedPoints
  members { playerId days isDefault }
  budget { enoughHistory basedOnCapacity sprintsUsed suggested low high pointsPerPersonDay }
  tickets { ticketId name externalKey externalUrl estimate points planned }`;
const ERRORS = `errors { ... on ApplicationError { code message } }`;

export const SPRINT_PLAN = gql`
  query SprintPlan($teamId: UUID!, $gameId: UUID!) {
    sprintPlan(teamId: $teamId, gameId: $gameId) { ... on SprintPlan { ${PLAN} } ... on ApplicationError { code message } }
  }`;
export const SET_SPRINT_DAYS = gql`
  mutation SetSprintDays($input: SetSprintDaysInput!) { setSprintDays(input: $input) { sprintPlan { ${PLAN} } ${ERRORS} } }`;
export const SET_MEMBER_CAPACITY = gql`
  mutation SetMemberCapacity($input: SetMemberCapacityInput!) { setMemberCapacity(input: $input) { sprintPlan { ${PLAN} } ${ERRORS} } }`;
export const SET_TICKET_PLANNED = gql`
  mutation SetTicketPlanned($input: SetTicketPlannedInput!) { setTicketPlanned(input: $input) { sprintPlan { ${PLAN} } ${ERRORS} } }`;
export const MOVE_PLAN_TO_JIRA_SPRINT = gql`
  mutation MovePlanToJiraSprint($input: MovePlanToJiraSprintInput!) { movePlanToJiraSprint(input: $input) { jiraSprintMoved { moved skipped } ${ERRORS} } }`;

interface MemberCapacity { playerId: string; days: number; isDefault: boolean }
interface PointBudget {
  enoughHistory: boolean; basedOnCapacity: boolean; sprintsUsed: number;
  suggested: number | null; low: number | null; high: number | null; pointsPerPersonDay: number | null;
}
interface PlanTicket {
  ticketId: string; name: string; externalKey: string | null; externalUrl: string | null;
  estimate: string | null; points: number | null; planned: boolean;
}
interface SprintPlan {
  gameId: string; sprintDays: number; sprintDaysSet: boolean; personDays: number; plannedPoints: number;
  members: MemberCapacity[]; budget: PointBudget; tickets: PlanTicket[];
}
type Payload = { sprintPlan?: SprintPlan | null; errors?: { message: string }[] } | undefined;

/**
 * Sprint capacity and plan in the team room (OPP-146, OPP-147, OPP-148, OPP-149): who is available, a point budget
 * from the team's own history, the tickets chosen for the sprint and the move into a Jira sprint. Team numbers only.
 */
@Component({
  selector: 'app-team-plan',
  standalone: true,
  imports: [TranslocoPipe, FormsModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="team-card" aria-labelledby="plan-title">
      <div class="plan-head">
        <h2 class="team-section-title" id="plan-title">{{ 'plan.title' | transloco }}</h2>
        @if (sprints.length > 1) {
          <label class="plan-select">
            <span class="sr">{{ 'plan.session' | transloco }}</span>
            <select [ngModel]="gameId" (ngModelChange)="selectSprint($event)" name="planSprint">
              @for (s of sprints; track s.gameId) { <option [value]="s.gameId">{{ s.name }}</option> }
            </select>
          </label>
        }
      </div>

      @if (!sprints.length) {
        <p class="muted">{{ 'plan.noSession' | transloco }}</p>
      } @else if (plan) {
        <!-- Capacity (OPP-146) -->
        <h3 class="sub">{{ 'plan.capacity' | transloco }}</h3>
        <div class="plan-row">
          <label for="sprint-days">{{ 'plan.sprintDays' | transloco }}</label>
          <input id="sprint-days" type="number" min="1" max="30" step="1" class="num" [disabled]="!isOwner"
            [ngModel]="plan.sprintDays" (change)="setSprintDays($any($event.target).valueAsNumber)">
          <span class="muted">{{ 'plan.workingDays' | transloco }}</span>
        </div>
        <div class="table-wrap">
          <table>
            <caption class="sr">{{ 'plan.capacity' | transloco }}</caption>
            <thead><tr><th scope="col">{{ 'team.members' | transloco }}</th><th scope="col">{{ 'plan.availableDays' | transloco }}</th></tr></thead>
            <tbody>
              @for (m of plan.members; track m.playerId) {
                <tr>
                  <th scope="row">{{ nameOf(m.playerId) }}@if (m.playerId === currentUserId) { <span class="muted"> ({{ 'plan.you' | transloco }})</span> }</th>
                  <td>
                    <input type="number" class="num" min="0" [max]="plan.sprintDays" step="0.5"
                      [attr.aria-label]="'plan.daysFor' | transloco: { name: nameOf(m.playerId) }"
                      [disabled]="!isOwner && m.playerId !== currentUserId"
                      [ngModel]="m.days" (change)="setMemberDays(m.playerId, $any($event.target).valueAsNumber)">
                  </td>
                </tr>
              }
            </tbody>
            <tfoot><tr><th scope="row">{{ 'plan.total' | transloco }}</th><td class="opp-tabular"><b>{{ plan.personDays }}</b> {{ 'plan.personDays' | transloco }}</td></tr></tfoot>
          </table>
        </div>

        <!-- Budget (OPP-147) -->
        <h3 class="sub">{{ 'plan.budget' | transloco }}</h3>
        @if (!plan.budget.enoughHistory) {
          <p class="muted">{{ 'plan.notEnoughHistory' | transloco }}</p>
        } @else {
          <p class="budget"><b class="opp-tabular">{{ plan.budget.suggested }}</b> {{ 'team.pts' | transloco }}
            <span class="muted">({{ 'plan.range' | transloco: { low: plan.budget.low, high: plan.budget.high } }})</span></p>
          <p class="muted">
            @if (plan.budget.basedOnCapacity) {
              {{ 'plan.howCapacity' | transloco: { count: plan.budget.sprintsUsed, rate: plan.budget.pointsPerPersonDay, days: plan.personDays } }}
            } @else {
              {{ 'plan.howAverage' | transloco: { count: plan.budget.sprintsUsed } }}
            }
          </p>
        }

        <!-- Plan (OPP-148) -->
        <h3 class="sub">{{ 'plan.tickets' | transloco }}</h3>
        @if (!plan.tickets.length) {
          <p class="muted">{{ 'plan.noTickets' | transloco }}</p>
        } @else {
          <div class="fill" [class.fill--over]="over > 0">
            <div class="fill-bar" role="meter" aria-valuemin="0" [attr.aria-valuemax]="budgetMax" [attr.aria-valuenow]="plan.plannedPoints"
              [attr.aria-label]="'plan.fillAria' | transloco"><span [style.width.%]="fillPercent"></span></div>
            <p class="fill-text opp-tabular" aria-live="polite">
              {{ 'plan.planned' | transloco: { points: plan.plannedPoints } }}
              @if (plan.budget.suggested !== null) { / {{ plan.budget.suggested }} {{ 'team.pts' | transloco }} }
              @if (over > 0) { <b class="warn">· {{ 'plan.over' | transloco: { points: over } }}</b> }
            </p>
          </div>
          <ul class="plan-list">
            @for (t of plan.tickets; track t.ticketId) {
              <li>
                <label class="plan-ticket" [class.plan-ticket--off]="!t.estimate">
                  <input type="checkbox" [checked]="t.planned" [disabled]="!isOwner" (change)="setPlanned(t, !t.planned)">
                  <span class="plan-name">@if (t.externalKey) { <b>{{ t.externalKey }}</b> } {{ t.name }}</span>
                  <span class="plan-est opp-tabular">{{ t.estimate ?? '–' }}</span>
                </label>
              </li>
            }
          </ul>
          <div class="plan-actions">
            <button mat-stroked-button (click)="copyPlan()" [disabled]="!plannedTickets.length">{{ 'plan.copy' | transloco }}</button>
            @if (isOwner && jiraPlanned) {
              <button mat-stroked-button (click)="loadBoards()">{{ 'plan.toJira' | transloco }}</button>
            }
          </div>

          <!-- Move into a Jira sprint (OPP-149) -->
          @if (boards) {
            <div class="jira">
              @if (jiraError) { <p class="error">{{ jiraError }}</p> }
              <label>{{ 'plan.board' | transloco }}
                <select [ngModel]="boardId" (ngModelChange)="selectBoard($event)" name="jiraBoard">
                  <option [ngValue]="null">–</option>
                  @for (b of boards; track b.id) { <option [ngValue]="b.id">{{ b.name }}</option> }
                </select>
              </label>
              @if (jiraSprints) {
                <label>{{ 'plan.jiraSprint' | transloco }}
                  <select [(ngModel)]="jiraSprintId" name="jiraSprint">
                    <option [ngValue]="null">–</option>
                    @for (s of jiraSprints; track s.id) { <option [ngValue]="s.id">{{ s.name }} ({{ s.state }})</option> }
                  </select>
                </label>
                @if (!jiraSprints.length) { <p class="muted">{{ 'plan.noJiraSprints' | transloco }}</p> }
              }
              <button mat-flat-button color="primary" [disabled]="!jiraSprintId || moving" (click)="moveToJira()">
                {{ 'plan.move' | transloco: { count: jiraPlanned } }}
              </button>
            </div>
          }
        }
      } @else if (error) {
        <p class="error">{{ error }}</p>
      }
      <p class="muted">{{ 'plan.hint' | transloco }}</p>
    </section>
  `,
  styles: [`
    :host { display: contents; }
    .team-card { display: grid; gap: var(--opp-space-3); align-content: start; padding: var(--opp-space-5); border: 1px solid var(--opp-border);
      border-radius: var(--opp-radius-lg); background: var(--opp-surface); min-width: 0; }
    .team-section-title { margin: 0; font-size: var(--opp-font-size-lg); }
    .plan-head { display: flex; flex-wrap: wrap; gap: var(--opp-space-3); align-items: center; justify-content: space-between; }
    .sub { margin: var(--opp-space-2) 0 0; font-size: var(--opp-font-size-base); }
    .muted { color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); margin: 0; }
    .error { color: var(--opp-danger); margin: 0; }
    .warn { color: var(--opp-spread); }
    select, .num { padding: 6px 8px; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md);
      background: var(--opp-surface); color: var(--opp-text); font: inherit; }
    select { max-width: 100%; }
    .num { width: 5.5em; }
    .plan-row { display: flex; flex-wrap: wrap; align-items: center; gap: var(--opp-space-2); }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; font-size: var(--opp-font-size-sm); }
    th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid var(--opp-border); }
    thead th { color: var(--opp-text-secondary); font-weight: 600; }
    tbody th, tfoot th { font-weight: 600; overflow-wrap: anywhere; }
    .budget { margin: 0; font-size: var(--opp-font-size-lg); }
    .budget b { font-family: var(--opp-font-display); font-size: var(--opp-font-size-2xl); color: var(--opp-primary); }
    .fill { display: grid; gap: 4px; }
    .fill-bar { height: 10px; border-radius: 5px; background: var(--opp-surface-hover); overflow: hidden; }
    .fill-bar span { display: block; height: 100%; border-radius: 5px; background: var(--opp-primary); }
    .fill--over .fill-bar span { background: var(--opp-spread); }
    .fill-text { margin: 0; font-size: var(--opp-font-size-sm); }
    .plan-list { list-style: none; margin: 0; padding: 0; display: grid; }
    .plan-list li { border-top: 1px solid var(--opp-border-light); }
    .plan-list li:first-child { border-top: 0; }
    .plan-ticket { display: flex; align-items: center; gap: var(--opp-space-2); min-height: 40px; font-size: var(--opp-font-size-sm); cursor: pointer; }
    .plan-ticket input { width: 18px; height: 18px; flex: none; accent-color: var(--opp-primary); }
    .plan-ticket--off { color: var(--opp-text-muted); }
    .plan-name { flex: 1; min-width: 0; overflow-wrap: anywhere; }
    .plan-est { flex: none; font-weight: 700; min-width: 2.5em; text-align: right; }
    .plan-actions, .jira { display: flex; flex-wrap: wrap; gap: var(--opp-space-2); align-items: center; }
    .jira label { display: grid; gap: 4px; font-size: var(--opp-font-size-sm); }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
  `],
})
export class TeamPlanComponent implements OnChanges {
  @Input() teamId = '';
  /** Planning sessions of the team, newest first. */
  @Input() sprints: Sprint[] = [];
  @Input() members: { id: string; name: string | null }[] = [];
  @Input() isOwner = false;
  @Input() currentUserId = '';

  gameId = '';
  plan: SprintPlan | null = null;
  error = '';
  boards: { id: number; name: string }[] | null = null;
  boardId: number | null = null;
  jiraSprints: { id: number; name: string; state: string }[] | null = null;
  jiraSprintId: number | null = null;
  jiraError = '';
  moving = false;

  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['sprints'] && this.sprints.length && !this.sprints.some(s => s.gameId === this.gameId)) {
      this.selectSprint(this.sprints[0].gameId);
    }
  }

  get plannedTickets(): PlanTicket[] {
    return this.plan?.tickets.filter(t => t.planned) ?? [];
  }

  /** Planned tickets that are Jira issues. */
  get jiraPlanned(): number {
    return this.plannedTickets.filter(t => t.externalKey && (trackerOfUrl(t.externalUrl) ?? 'jira') === 'jira').length;
  }

  get budgetMax(): number {
    return Math.max(this.plan?.budget.suggested ?? 0, this.plan?.plannedPoints ?? 0, 1);
  }

  get fillPercent(): number {
    if (!this.plan) return 0;
    const target = this.plan.budget.suggested ?? this.plan.plannedPoints;
    return target > 0 ? Math.min(100, (this.plan.plannedPoints / target) * 100) : 0;
  }

  get over(): number {
    const suggested = this.plan?.budget.suggested;
    return suggested === null || suggested === undefined ? 0 : Math.max(0, Math.round(((this.plan!.plannedPoints) - suggested) * 10) / 10);
  }

  nameOf(playerId: string): string {
    return this.members.find(m => m.id === playerId)?.name ?? this.i18n.translate('plan.unknownMember');
  }

  selectSprint(gameId: string): void {
    this.gameId = gameId;
    this.plan = null;
    this.error = '';
    this.apollo.query<{ sprintPlan: SprintPlan & { code?: string; message?: string } }>({
      query: SPRINT_PLAN, variables: { teamId: this.teamId, gameId }, fetchPolicy: 'network-only',
    }).subscribe({
      next: ({ data }) => {
        const plan = data?.sprintPlan;
        if (plan?.code || !plan) this.error = plan?.message ?? this.i18n.translate('common.tryAgainNetwork');
        else if (gameId === this.gameId) this.plan = plan;
        this.cdr.markForCheck();
      },
      error: () => { this.error = this.i18n.translate('common.tryAgainNetwork'); this.cdr.markForCheck(); },
    });
  }

  setSprintDays(days: number): void {
    if (!Number.isFinite(days)) return;
    this.save(SET_SPRINT_DAYS, 'setSprintDays', { teamId: this.teamId, gameId: this.gameId, days: Math.round(days) });
  }

  setMemberDays(playerId: string, days: number): void {
    if (!Number.isFinite(days)) return;
    this.save(SET_MEMBER_CAPACITY, 'setMemberCapacity', { teamId: this.teamId, gameId: this.gameId, playerId, days });
  }

  setPlanned(ticket: PlanTicket, planned: boolean): void {
    // Show it at once (Apollo results are read-only, so copy); the answer brings the new total.
    if (this.plan) {
      this.plan = { ...this.plan, tickets: this.plan.tickets.map(t => t.ticketId === ticket.ticketId ? { ...t, planned } : t) };
    }
    this.save(SET_TICKET_PLANNED, 'setTicketPlanned', { teamId: this.teamId, ticketId: ticket.ticketId, planned });
  }

  /** The plan as plain text (key, title, points) for a chat or a document. */
  copyPlan(): void {
    const lines = this.plannedTickets.map(t => `${t.externalKey ? t.externalKey + ' ' : ''}${t.name} — ${t.estimate ?? '–'}`);
    const total = this.i18n.translate('plan.planned', { points: this.plan?.plannedPoints ?? 0 });
    navigator.clipboard?.writeText([...lines, '', total].join('\n'))
      .then(() => this.notify(this.i18n.translate('plan.copied')))
      .catch(() => this.notify(this.i18n.translate('common.tryAgainNetwork')));
  }

  loadBoards(): void {
    this.jiraError = '';
    this.apollo.query<{ jiraBoards: { items?: { id: number; name: string }[]; message?: string } }>({ query: GET_JIRA_BOARDS, fetchPolicy: 'network-only' })
      .subscribe({
        next: ({ data }) => {
          const result = data?.jiraBoards;
          this.boards = result?.items ?? [];
          if (!result?.items) this.jiraError = result?.message ?? this.i18n.translate('plan.connectJira');
          this.cdr.markForCheck();
        },
        error: () => { this.boards = []; this.jiraError = this.i18n.translate('common.tryAgainNetwork'); this.cdr.markForCheck(); },
      });
  }

  selectBoard(boardId: number | null): void {
    this.boardId = boardId;
    this.jiraSprints = null;
    this.jiraSprintId = null;
    if (boardId === null) return;
    this.apollo.query<{ jiraSprints: { items?: { id: number; name: string; state: string }[]; message?: string } }>({
      query: GET_JIRA_SPRINTS, variables: { boardId }, fetchPolicy: 'network-only',
    }).subscribe({
      next: ({ data }) => {
        this.jiraSprints = data?.jiraSprints?.items ?? [];
        if (!data?.jiraSprints?.items) this.jiraError = data?.jiraSprints?.message ?? '';
        this.cdr.markForCheck();
      },
      error: () => { this.jiraSprints = []; this.cdr.markForCheck(); },
    });
  }

  moveToJira(): void {
    if (!this.jiraSprintId) return;
    this.moving = true;
    this.apollo.mutate<{ movePlanToJiraSprint: { jiraSprintMoved?: { moved: number; skipped: number }; errors?: { message: string }[] } }>({
      mutation: MOVE_PLAN_TO_JIRA_SPRINT, variables: { input: { teamId: this.teamId, gameId: this.gameId, sprintId: this.jiraSprintId } },
    }).subscribe({
      next: ({ data }) => {
        this.moving = false;
        const payload = data?.movePlanToJiraSprint;
        if (payload?.errors?.length) this.notify(payload.errors[0].message);
        else this.notify(this.i18n.translate('plan.moved', { count: payload?.jiraSprintMoved?.moved ?? 0, skipped: payload?.jiraSprintMoved?.skipped ?? 0 }));
        this.cdr.markForCheck();
      },
      error: () => { this.moving = false; this.notify(this.i18n.translate('common.tryAgainNetwork')); this.cdr.markForCheck(); },
    });
  }

  private save(mutation: ReturnType<typeof gql>, field: string, input: Record<string, unknown>): void {
    this.apollo.mutate<Record<string, Payload>>({ mutation, variables: { input } }).subscribe({
      next: ({ data }) => {
        const payload = data?.[field];
        if (payload?.errors?.length) {
          this.notify(payload.errors[0].message);
          this.selectSprint(this.gameId);
          return;
        }
        if (payload?.sprintPlan) this.plan = payload.sprintPlan;
        this.cdr.markForCheck();
      },
      error: () => { this.notify(this.i18n.translate('common.tryAgainNetwork')); this.selectSprint(this.gameId); },
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, this.i18n.translate('common.close'), { duration: OPP_SNACKBAR_DURATION_DEFAULT * 2 });
  }
}
