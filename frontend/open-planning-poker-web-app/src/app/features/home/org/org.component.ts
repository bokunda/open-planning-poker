import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Apollo, gql } from 'apollo-angular';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OPP_SNACKBAR_DURATION_DEFAULT } from '../../../shared/constants';
import { MY_TEAMS, TeamSummary } from '../team/team.graphql';
import { comparisonCsv, TeamComparison } from './org-helpers';

const ERRORS = `errors { ... on ApplicationError { code message } }`;
const ORG_FIELDS = `id name myRole inviteToken`;

export const MY_ORGANIZATIONS = gql`query MyOrganizations { myOrganizations { ${ORG_FIELDS} } }`;
export const GET_ORGANIZATION = gql`
  query Organization($id: UUID!) {
    organization(id: $id) {
      ... on OrganizationInfo { ${ORG_FIELDS} members { id name role } teams { id name ownerId }
        comparison { teamId name sprints firstRoundConsensusShare reVotesPerTicket averageVelocity velocity } }
      ... on ApplicationError { code message }
    }
  }`;
const CREATE = gql`mutation CreateOrganization($input: CreateOrganizationInput!) { createOrganization(input: $input) { organizationInfo { id } ${ERRORS} } }`;
const JOIN = gql`mutation JoinOrganization($input: JoinOrganizationInput!) { joinOrganization(input: $input) { boolean ${ERRORS} } }`;
const RENAME = gql`mutation RenameOrganization($input: RenameOrganizationInput!) { renameOrganization(input: $input) { boolean ${ERRORS} } }`;
const ROTATE = gql`mutation RotateOrganizationInvite($input: RotateOrganizationInviteInput!) { rotateOrganizationInvite(input: $input) { organizationInfo { id } ${ERRORS} } }`;
const SET_ROLE = gql`mutation SetOrganizationRole($input: SetOrganizationRoleInput!) { setOrganizationRole(input: $input) { boolean ${ERRORS} } }`;
const REMOVE_MEMBER = gql`mutation RemoveOrganizationMember($input: RemoveOrganizationMemberInput!) { removeOrganizationMember(input: $input) { boolean ${ERRORS} } }`;
const ADD_TEAM = gql`mutation AddTeamToOrganization($input: AddTeamToOrganizationInput!) { addTeamToOrganization(input: $input) { boolean ${ERRORS} } }`;
const REMOVE_TEAM = gql`mutation RemoveTeamFromOrganization($input: RemoveTeamFromOrganizationInput!) { removeTeamFromOrganization(input: $input) { boolean ${ERRORS} } }`;

type Role = 'ADMIN' | 'MEMBER';
interface Org {
  id: string; name: string; myRole: Role | null; inviteToken: string | null;
  members?: { id: string; name: string | null; role: Role }[];
  teams?: { id: string; name: string; ownerId: string }[];
  comparison?: TeamComparison[];
}
type Payload = { errors?: { code: string; message: string }[] | null } | null | undefined;

/**
 * Organizations (OPP-154): several team rooms of one company. /org lists yours and creates one; /org/:id is the
 * organization itself, joined with its invite link. Admins see every team's numbers side by side (OPP-157).
 */
@Component({
  selector: 'app-org',
  standalone: true,
  imports: [TranslocoPipe, FormsModule, RouterLink, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="org">
      @if (!orgId) {
        <section class="card" aria-labelledby="orgs-title">
          <h1 id="orgs-title">{{ 'org.title' | transloco }}</h1>
          <p class="muted">{{ 'org.lead' | transloco }}</p>
          @if (orgs.length) {
            <ul class="list">
              @for (o of orgs; track o.id) {
                <li><a [routerLink]="['/org', o.id]">{{ o.name }}</a> <span class="muted">{{ ('org.role.' + o.myRole) | transloco }}</span></li>
              }
            </ul>
          }
          <form class="row" (ngSubmit)="create()">
            <label class="sr" for="org-name">{{ 'org.name' | transloco }}</label>
            <input id="org-name" name="orgName" maxlength="120" [(ngModel)]="newName" [placeholder]="'org.namePlaceholder' | transloco">
            <button mat-flat-button color="primary" type="submit" [disabled]="!newName.trim() || busy">{{ 'org.create' | transloco }}</button>
          </form>
        </section>
      } @else if (loading) {
        <p class="muted">{{ 'org.loading' | transloco }}</p>
      } @else if (!org) {
        <p class="muted">{{ 'org.notFound' | transloco }}</p>
      } @else {
        <section class="card" aria-labelledby="org-title">
          <h1 id="org-title">{{ org.name }}</h1>
          @if (!org.myRole) {
            <p>{{ 'org.invited' | transloco }}</p>
            @if (inviteToken) {
              <button mat-flat-button color="primary" (click)="join()" [disabled]="busy">{{ 'org.join' | transloco: { name: org.name } }}</button>
            } @else {
              <p class="muted">{{ 'org.needLink' | transloco }}</p>
            }
          } @else {
            <p class="muted">{{ ('org.role.' + org.myRole) | transloco }}</p>
            @if (isAdmin) {
              <form class="row" (ngSubmit)="rename()">
                <label class="sr" for="org-rename">{{ 'org.name' | transloco }}</label>
                <input id="org-rename" name="rename" maxlength="120" [(ngModel)]="renameDraft">
                <button mat-stroked-button type="submit" [disabled]="!renameDraft.trim() || renameDraft === org.name">{{ 'org.rename' | transloco }}</button>
              </form>
              <div class="invite">
                <label for="org-invite"><strong>{{ 'org.inviteLink' | transloco }}</strong></label>
                <input id="org-invite" readonly [value]="inviteUrl">
                <div class="row">
                  <button mat-stroked-button type="button" (click)="copy(inviteUrl)">{{ 'org.copy' | transloco }}</button>
                  <button mat-button type="button" (click)="rotate()">{{ 'org.newLink' | transloco }}</button>
                </div>
              </div>
            }
          }
        </section>

        @if (org.myRole) {
          <section class="card" aria-labelledby="teams-title">
            <h2 id="teams-title">{{ 'org.teams' | transloco }}</h2>
            @if (!org.teams?.length) { <p class="muted">{{ 'org.noTeams' | transloco }}</p> }
            <ul class="list">
              @for (t of org.teams; track t.id) {
                <li>
                  <a [routerLink]="['/team', t.id]">{{ t.name }}</a>
                  @if (isAdmin || t.ownerId === currentUserId) {
                    <button mat-button type="button" (click)="removeTeam(t.id)">{{ 'org.removeTeam' | transloco }}</button>
                  }
                </li>
              }
            </ul>
            @if (movableTeams.length) {
              <form class="row" (ngSubmit)="addTeam()">
                <label class="sr" for="org-add-team">{{ 'org.addTeam' | transloco }}</label>
                <select id="org-add-team" name="addTeam" [(ngModel)]="teamToAdd">
                  <option value="">{{ 'org.pickTeam' | transloco }}</option>
                  @for (t of movableTeams; track t.id) { <option [value]="t.id">{{ t.name }}</option> }
                </select>
                <button mat-stroked-button type="submit" [disabled]="!teamToAdd">{{ 'org.addTeam' | transloco }}</button>
              </form>
              <p class="muted">{{ 'org.addTeamHint' | transloco }}</p>
            }
          </section>

          @if (isAdmin) {
            <section class="card" aria-labelledby="compare-title">
              <h2 id="compare-title">{{ 'org.compare' | transloco }}</h2>
              <p class="muted">{{ 'org.compareLead' | transloco }}</p>
              @if (org.comparison?.length) {
                <div class="table-wrap">
                  <table>
                    <caption class="sr">{{ 'org.compare' | transloco }}</caption>
                    <thead><tr>
                      <th scope="col">{{ 'org.team' | transloco }}</th><th scope="col">{{ 'org.sprints' | transloco }}</th>
                      <th scope="col">{{ 'org.firstVote' | transloco }}</th><th scope="col">{{ 'org.reVotes' | transloco }}</th>
                      <th scope="col">{{ 'org.velocity' | transloco }}</th><th scope="col">{{ 'org.trend' | transloco }}</th>
                    </tr></thead>
                    <tbody>
                      @for (c of org.comparison; track c.teamId) {
                        <tr>
                          <th scope="row">{{ c.name }}</th>
                          <td class="num">{{ c.sprints }}</td>
                          <td class="num">{{ c.firstRoundConsensusShare == null ? '–' : (c.firstRoundConsensusShare * 100).toFixed(0) + '%' }}</td>
                          <td class="num">{{ c.reVotesPerTicket ?? '–' }}</td>
                          <td class="num">{{ c.averageVelocity ?? '–' }}</td>
                          <td><span class="spark" [attr.aria-label]="c.velocity.join(', ')">
                            @for (v of c.velocity; track $index) { <span [style.height.%]="bar(v, c.velocity)"></span> }
                          </span></td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
                <button mat-stroked-button type="button" (click)="downloadCsv()">{{ 'org.csv' | transloco }}</button>
              } @else {
                <p class="muted">{{ 'org.noNumbers' | transloco }}</p>
              }
            </section>
          }

          <section class="card" aria-labelledby="members-title">
            <h2 id="members-title">{{ 'org.members' | transloco }}</h2>
            <ul class="list">
              @for (m of org.members; track m.id) {
                <li>
                  <span>{{ m.name || ('org.someone' | transloco) }}@if (m.id === currentUserId) { <span class="muted"> · {{ 'org.you' | transloco }}</span> }</span>
                  @if (isAdmin) {
                    <span class="row">
                      <select [attr.aria-label]="'org.roleLabel' | transloco" [ngModel]="m.role" (ngModelChange)="setRole(m.id, $event)">
                        <option value="MEMBER">{{ 'org.role.MEMBER' | transloco }}</option>
                        <option value="ADMIN">{{ 'org.role.ADMIN' | transloco }}</option>
                      </select>
                      @if (m.id !== currentUserId) { <button mat-button type="button" (click)="removeMember(m.id)">{{ 'org.remove' | transloco }}</button> }
                    </span>
                  } @else {
                    <span class="muted">{{ ('org.role.' + m.role) | transloco }}</span>
                  }
                </li>
              }
            </ul>
            <button mat-button type="button" (click)="leave()">{{ 'org.leave' | transloco }}</button>
          </section>
        }
      }
    </div>
  `,
  styles: [`
    :host { display: block; width: 100%; }
    .org { display: grid; gap: var(--opp-space-4); max-width: 960px; margin: 0 auto; padding: var(--opp-space-4); box-sizing: border-box; }
    .card { display: grid; gap: var(--opp-space-3); padding: var(--opp-space-5); border: 1px solid var(--opp-border); border-radius: var(--opp-radius-lg);
      background: var(--opp-surface); min-width: 0; }
    h1, h2 { margin: 0; } h1 { font-size: var(--opp-font-size-2xl); } h2 { font-size: var(--opp-font-size-lg); }
    .muted { margin: 0; color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); }
    .row { display: flex; flex-wrap: wrap; align-items: center; gap: var(--opp-space-2); }
    .list { list-style: none; margin: 0; padding: 0; display: grid; gap: var(--opp-space-1); }
    .list li { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: var(--opp-space-2);
      padding: var(--opp-space-2) 0; border-bottom: 1px solid var(--opp-border); }
    a { color: var(--opp-primary); font-weight: 600; }
    input, select { font: inherit; color: var(--opp-text); background: var(--opp-bg); border: 1px solid var(--opp-border);
      border-radius: var(--opp-radius-md); padding: 8px 10px; min-width: 0; }
    .row input { flex: 1 1 220px; }
    .invite { display: grid; gap: var(--opp-space-2); } .invite input { width: 100%; box-sizing: border-box; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; font-size: var(--opp-font-size-sm); }
    th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid var(--opp-border); white-space: nowrap; }
    .num { font-variant-numeric: tabular-nums; }
    .spark { display: inline-flex; align-items: flex-end; gap: 2px; height: 24px; }
    .spark span { width: 6px; min-height: 2px; background: var(--opp-primary); border-radius: 1px; }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
  `],
})
export class OrgComponent implements OnInit {
  private _currentUserId = '';
  @Input()
  set currentUserId(value: string) {
    const first = !this._currentUserId && !!value;
    this._currentUserId = value;
    if (first) this.load();
  }
  get currentUserId(): string { return this._currentUserId; }

  private readonly apollo = inject(Apollo);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);
  private readonly cdr = inject(ChangeDetectorRef);

  orgId: string | null = null;
  inviteToken: string | null = null;
  orgs: Org[] = [];
  org: Org | null = null;
  myTeams: TeamSummary[] = [];
  loading = true;
  busy = false;
  newName = '';
  renameDraft = '';
  teamToAdd = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.orgId = params.get('orgId');
      this.inviteToken = this.route.snapshot.queryParamMap.get('invite');
      if (this.currentUserId) this.load();
    });
  }

  get isAdmin(): boolean { return this.org?.myRole === 'ADMIN'; }

  get inviteUrl(): string {
    return this.org?.inviteToken && typeof window !== 'undefined' ? `${window.location.origin}/org/${this.org.id}?invite=${this.org.inviteToken}` : '';
  }

  /** The caller's own teams that are not in this organization yet. */
  get movableTeams(): TeamSummary[] {
    const inOrg = new Set(this.org?.teams?.map(t => t.id) ?? []);
    return this.myTeams.filter(t => t.ownerId === this.currentUserId && !inOrg.has(t.id));
  }

  bar(value: number, all: number[]): number {
    const max = Math.max(...all, 1);
    return Math.max(8, Math.round(value / max * 100));
  }

  private load(): void {
    this.loading = true;
    if (!this.orgId) {
      this.apollo.query<{ myOrganizations: Org[] }>({ query: MY_ORGANIZATIONS, fetchPolicy: 'no-cache' }).subscribe(({ data }) => {
        this.orgs = data?.myOrganizations ?? [];
        this.loading = false;
        this.cdr.markForCheck();
      });
      return;
    }
    this.apollo.query<{ organization: Org & { code?: string } }>({ query: GET_ORGANIZATION, variables: { id: this.orgId }, fetchPolicy: 'no-cache' })
      .subscribe(({ data }) => {
        this.org = data?.organization?.id ? data.organization : null;
        this.renameDraft = this.org?.name ?? '';
        this.loading = false;
        this.cdr.markForCheck();
      });
    this.apollo.query<{ myTeams: TeamSummary[] }>({ query: MY_TEAMS, fetchPolicy: 'no-cache' }).subscribe(({ data }) => {
      this.myTeams = data?.myTeams ?? [];
      this.cdr.markForCheck();
    });
  }

  create(): void {
    const name = this.newName.trim();
    if (!name) return;
    this.run<{ organizationInfo?: { id: string } }>(CREATE, 'createOrganization', { name }, r => {
      if (r?.organizationInfo) this.router.navigate(['/org', r.organizationInfo.id]);
    });
  }

  join(): void {
    this.run(JOIN, 'joinOrganization', { id: this.orgId, token: this.inviteToken }, () => {
      this.router.navigate(['/org', this.orgId], { replaceUrl: true });
      this.load();
    });
  }

  rename(): void { this.run(RENAME, 'renameOrganization', { id: this.orgId, name: this.renameDraft.trim() }, () => this.load()); }
  rotate(): void { this.run(ROTATE, 'rotateOrganizationInvite', { id: this.orgId }, () => this.load()); }
  setRole(playerId: string, role: Role): void { this.run(SET_ROLE, 'setOrganizationRole', { id: this.orgId, playerId, role }, () => this.load(), () => this.load()); }
  removeMember(playerId: string): void { this.run(REMOVE_MEMBER, 'removeOrganizationMember', { id: this.orgId, playerId }, () => this.load()); }
  leave(): void { this.run(REMOVE_MEMBER, 'removeOrganizationMember', { id: this.orgId, playerId: this.currentUserId }, () => this.router.navigate(['/org'])); }

  addTeam(): void {
    if (!this.teamToAdd) return;
    this.run(ADD_TEAM, 'addTeamToOrganization', { id: this.orgId, teamId: this.teamToAdd }, () => { this.teamToAdd = ''; this.load(); });
  }

  removeTeam(teamId: string): void { this.run(REMOVE_TEAM, 'removeTeamFromOrganization', { id: this.orgId, teamId }, () => this.load()); }

  downloadCsv(): void {
    if (!this.org?.comparison) return;
    const blob = new Blob([comparisonCsv(this.org.comparison)], { type: 'text/csv;charset=utf-8' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `${this.org.name.replace(/[^\p{L}\p{N}]+/gu, '-')}-teams.csv`;
    a.click();
    setTimeout(() => URL.revokeObjectURL(a.href), 1000);
  }

  copy(value: string): void {
    navigator.clipboard?.writeText(value).then(() => this.notify(this.i18n.translate('org.copied')));
  }

  private run<T extends object>(mutation: ReturnType<typeof gql>, field: string, input: Record<string, unknown>, done?: (r: T | undefined) => void, failed?: () => void): void {
    this.busy = true;
    this.apollo.mutate<Record<string, Payload & T>>({ mutation, variables: { input } }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        const result = data?.[field];
        if (result?.errors?.length) { this.notify(result.errors[0].message); failed?.(); }
        else done?.(result as T | undefined);
        this.cdr.markForCheck();
      },
      error: () => { this.busy = false; this.notify(this.i18n.translate('common.tryAgainNetwork')); this.cdr.markForCheck(); },
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, undefined, { duration: OPP_SNACKBAR_DURATION_DEFAULT });
  }
}
