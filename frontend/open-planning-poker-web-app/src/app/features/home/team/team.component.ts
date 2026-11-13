import { ChangeDetectorRef, Component, inject, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Apollo } from 'apollo-angular';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OPP_SNACKBAR_DURATION_DEFAULT, OPP_SNACKBAR_MODAL_LABEL_CLOSE } from '../../../shared/constants';
import { CREATE_SPRINT, CREATE_TEAM, GET_TEAM, JOIN_TEAM, LEAVE_TEAM, MY_TEAMS, TeamDetails, TeamSummary } from './team.graphql';
import { VelocityChartComponent } from './velocity-chart.component';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OppDatePipe } from '../../../i18n/opp-date.pipe';

type Errors = { errors?: { message: string }[] | null } | null | undefined;

/**
 * Team rooms: /team lists your teams and creates one; /team/:teamId is the room itself
 * (invite link, members, start a sprint planning, sprint history and velocity).
 */
@Component({
  selector: 'app-team',
  standalone: true,
  imports: [TranslocoPipe, OppDatePipe, CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule, VelocityChartComponent],
  templateUrl: './team.component.html',
  styleUrl: './team.component.scss',
})
export class TeamComponent implements OnInit {
  private _currentUserId = '';
  /** Queries need the user's token, so they run once the user is known. */
  @Input()
  set currentUserId(value: string) {
    const first = !this._currentUserId && !!value;
    this._currentUserId = value;
    if (first) this.load();
  }
  get currentUserId(): string {
    return this._currentUserId;
  }

  private readonly apollo = inject(Apollo);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);
  private readonly cdr = inject(ChangeDetectorRef);

  teamId: string | null = null;
  team: TeamDetails | null = null;
  teams: TeamSummary[] = [];
  loading = true;
  notFound = false;
  busy = false;

  newTeamName = '';
  newTeamDescription = '';
  sprintName = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.teamId = params.get('teamId');
      this.team = null;
      this.notFound = false;
      if (this.currentUserId) this.load();
    });
  }

  get isOwner(): boolean {
    return !!this.team && this.team.ownerId === this.currentUserId;
  }

  get inviteUrl(): string {
    return typeof window === 'undefined' ? '' : `${window.location.origin}/team/${this.teamId}`;
  }

  private load(): void {
    this.loading = true;
    if (this.teamId) {
      this.apollo.query<{ team: TeamDetails & { code?: string } }>({ query: GET_TEAM, variables: { id: this.teamId } }).subscribe({
        next: ({ data }) => {
          this.loading = false;
          this.notFound = !data?.team?.id;
          this.team = data?.team?.id ? data.team : null;
          this.sprintName ||= this.defaultSprintName();
          this.cdr.markForCheck();
        },
        error: () => { this.loading = false; this.notFound = true; }
      });
    } else {
      this.apollo.query<{ myTeams: TeamSummary[] }>({ query: MY_TEAMS }).subscribe({
        next: ({ data }) => {
          this.loading = false;
          this.teams = data?.myTeams ?? [];
          this.cdr.markForCheck();
        },
        error: () => { this.loading = false; }
      });
    }
  }

  createTeam(): void {
    const name = this.newTeamName.trim();
    if (!name || this.busy) return;
    this.busy = true;
    this.apollo.mutate<{ createTeam: Errors & { team?: TeamSummary } }>({
      mutation: CREATE_TEAM,
      variables: { input: { name, description: this.newTeamDescription.trim() } }
    }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        const team = data?.createTeam?.team;
        if (this.failed(data?.createTeam) || !team) return;
        this.newTeamName = '';
        this.newTeamDescription = '';
        this.router.navigate(['/team', team.id]);
      },
      error: () => { this.busy = false; this.notify(this.i18n.translate('team.createFailed')); }
    });
  }

  joinTeam(): void {
    if (!this.teamId || this.busy) return;
    this.busy = true;
    this.apollo.mutate<{ joinTeam: Errors }>({ mutation: JOIN_TEAM, variables: { input: { teamId: this.teamId } } }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        if (!this.failed(data?.joinTeam)) this.load();
      },
      error: () => { this.busy = false; this.notify(this.i18n.translate('team.joinFailed')); }
    });
  }

  leaveTeam(): void {
    if (!this.teamId || this.busy) return;
    this.busy = true;
    this.apollo.mutate<{ leaveTeam: Errors }>({ mutation: LEAVE_TEAM, variables: { input: { teamId: this.teamId } } }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        if (!this.failed(data?.leaveTeam)) this.router.navigate(['/team']);
      },
      error: () => { this.busy = false; }
    });
  }

  /** A sprint planning is a new game in this team; its creator becomes the host. */
  startSprint(): void {
    const name = this.sprintName.trim() || this.defaultSprintName();
    if (!this.teamId || this.busy) return;
    this.busy = true;
    this.apollo.mutate<{ createGame: Errors & { game?: { id: string } } }>({
      mutation: CREATE_SPRINT,
      variables: { input: { name, description: this.team?.name ?? '', teamId: this.teamId } }
    }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        const game = data?.createGame?.game;
        if (this.failed(data?.createGame) || !game) return;
        this.router.navigate(['/game', game.id]);
      },
      error: () => { this.busy = false; this.notify(this.i18n.translate('team.startFailed')); }
    });
  }

  copyInvite(): void {
    navigator.clipboard.writeText(this.inviteUrl)
      .then(() => this.notify(this.i18n.translate('team.linkCopied')))
      .catch(() => this.notify(this.i18n.translate('room.copyFailed')));
  }

  private defaultSprintName(): string {
    const date = new Date().toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
    return this.i18n.translate('team.defaultSprintName', { date });
  }

  private failed(payload: Errors): boolean {
    const message = payload?.errors?.[0]?.message;
    if (message) this.notify(message);
    return !!message;
  }

  private notify(message: string): void {
    this.snackBar.open(message, this.i18n.translate('common.close'), { duration: OPP_SNACKBAR_DURATION_DEFAULT * 2 });
  }
}
