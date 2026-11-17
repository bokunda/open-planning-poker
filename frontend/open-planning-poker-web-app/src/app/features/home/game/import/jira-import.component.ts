import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { DOCUMENT } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoPipe } from '@jsverse/transloco';
import { Apollo } from 'apollo-angular';
import {
  AppError, DISCONNECT_JIRA, GET_JIRA_AUTHORIZE_URL, GET_JIRA_BOARDS, GET_JIRA_CONNECTION, GET_JIRA_ISSUES,
  GET_JIRA_SPRINTS, IMPORT_JIRA_ISSUES, JiraBoard, JiraConnectionStatus, JiraIssue, JiraSprint
} from '../gql/jira.graphql';

type Source = 'sprint' | 'jql';

/**
 * "From Jira" in the import dialog (OPP-11): connect Jira, pick a board and sprint (or type JQL), check the
 * preview and import. The server skips issues that are already tickets of the game.
 */
@Component({
  selector: 'app-jira-import',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatProgressSpinnerModule, TranslocoPipe],
  template: `
    @if (loading) {
      <div class="jira-loading"><mat-spinner diameter="28"></mat-spinner></div>
    } @else if (!status?.enabled) {
      <p class="jira-note">{{ 'jira.notAvailable' | transloco }}</p>
    } @else if (!status?.connected) {
      <div class="jira-connect">
        <p class="jira-note">{{ (status?.needsReconnect ? 'jira.reconnectHint' : 'jira.connectHint') | transloco }}</p>
        <button mat-flat-button color="primary" (click)="connect()" [disabled]="connecting">
          {{ (status?.needsReconnect ? 'jira.reconnect' : 'jira.connect') | transloco }}
        </button>
      </div>
    } @else {
      <div class="jira-site">
        <span>{{ 'jira.connectedTo' | transloco: { site: status?.siteName || '' } }}</span>
        <button mat-button (click)="disconnect()">{{ 'jira.disconnect' | transloco }}</button>
      </div>

      <div class="jira-source" role="radiogroup" [attr.aria-label]="'jira.source' | transloco">
        <label><input type="radio" name="jira-source" [checked]="source === 'sprint'" (change)="source = 'sprint'"> {{ 'jira.fromSprint' | transloco }}</label>
        <label><input type="radio" name="jira-source" [checked]="source === 'jql'" (change)="source = 'jql'"> {{ 'jira.fromJql' | transloco }}</label>
      </div>

      @if (source === 'sprint') {
        <div class="jira-row">
          <mat-form-field appearance="outline">
            <mat-label>{{ 'jira.board' | transloco }}</mat-label>
            <mat-select [(ngModel)]="boardId" (selectionChange)="onBoardChange()">
              @for (board of boards; track board.id) {
                <mat-option [value]="board.id">{{ board.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'jira.sprint' | transloco }}</mat-label>
            <mat-select [(ngModel)]="sprintId" [disabled]="!boardId">
              <mat-option [value]="null">{{ 'jira.openIssues' | transloco }}</mat-option>
              @for (sprint of sprints; track sprint.id) {
                <mat-option [value]="sprint.id">{{ sprint.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </div>
      } @else {
        <mat-form-field appearance="outline" class="jira-jql">
          <mat-label>JQL</mat-label>
          <input matInput [(ngModel)]="jql" placeholder="project = OPP AND sprint in openSprints()" (keydown.enter)="preview()">
        </mat-form-field>
      }

      <button mat-stroked-button (click)="preview()" [disabled]="searching || (source === 'sprint' ? !boardId : !jql.trim())">
        {{ 'jira.preview' | transloco }}
      </button>

      @if (issues.length > 0) {
        <div class="jira-preview">
          @if (truncated) {
            <p class="jira-note">{{ 'jira.truncated' | transloco: { max: max } }}</p>
          }
          <ul class="jira-issues">
            @for (issue of issues; track issue.key) {
              <li [class.jira-issue--done]="issue.alreadyInGame">
                <label>
                  <input type="checkbox" [disabled]="issue.alreadyInGame" [checked]="selected.has(issue.key)" (change)="toggle(issue.key)">
                  <b>{{ issue.key }}</b> {{ issue.summary }}
                </label>
                <span class="jira-points">
                  @if (issue.alreadyInGame) { {{ 'jira.inGame' | transloco }} }
                  @else if (issue.storyPoints != null) { {{ issue.storyPoints }} }
                </span>
              </li>
            }
          </ul>
        </div>
      } @else if (searched && !searching) {
        <p class="jira-note">{{ 'jira.noIssues' | transloco }}</p>
      }

      <div class="jira-actions">
        <button mat-flat-button color="primary" (click)="import()" [disabled]="selected.size === 0 || importing">
          {{ 'jira.import' | transloco: { count: selected.size } }}
        </button>
      </div>
    }
    @if (error) {
      <p class="jira-error" role="alert">{{ error }}</p>
    }
  `,
  styles: [`
    :host { display: grid; gap: var(--opp-space-3); padding: var(--opp-space-3) 0; }
    .jira-loading { display: grid; place-items: center; padding: var(--opp-space-5); }
    .jira-note { margin: 0; color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); }
    .jira-connect { display: grid; gap: var(--opp-space-3); justify-items: start; }
    .jira-site { display: flex; align-items: center; justify-content: space-between; gap: var(--opp-space-2); font-size: var(--opp-font-size-sm); }
    .jira-source { display: flex; gap: var(--opp-space-4); font-size: var(--opp-font-size-sm); }
    .jira-source label { display: inline-flex; align-items: center; gap: var(--opp-space-1); cursor: pointer; }
    .jira-row { display: grid; grid-template-columns: 1fr 1fr; gap: var(--opp-space-2); }
    .jira-jql { width: 100%; }
    .jira-issues { list-style: none; margin: 0; padding: 0; max-height: 240px; overflow: auto; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md); }
    .jira-issues li { display: flex; justify-content: space-between; gap: var(--opp-space-2); padding: var(--opp-space-2) var(--opp-space-3); border-bottom: 1px solid var(--opp-border); font-size: var(--opp-font-size-sm); }
    .jira-issues li:last-child { border-bottom: 0; }
    .jira-issues label { display: flex; gap: var(--opp-space-2); align-items: baseline; cursor: pointer; }
    .jira-issue--done { color: var(--opp-text-muted); }
    .jira-points { white-space: nowrap; color: var(--opp-text-muted); }
    .jira-actions { display: flex; justify-content: flex-end; }
    .jira-error { margin: 0; color: var(--opp-danger); font-size: var(--opp-font-size-sm); }
    @media (max-width: 480px) { .jira-row { grid-template-columns: 1fr; } }
  `]
})
export class JiraImportComponent implements OnInit {
  @Input({ required: true }) gameId!: string;
  /** Number of tickets that were added. */
  @Output() imported = new EventEmitter<number>();

  private readonly apollo = inject(Apollo);
  private readonly document = inject(DOCUMENT);

  loading = true;
  connecting = false;
  searching = false;
  searched = false;
  importing = false;
  status: JiraConnectionStatus | null = null;
  error = '';

  source: Source = 'sprint';
  boards: JiraBoard[] = [];
  sprints: JiraSprint[] = [];
  boardId: number | null = null;
  sprintId: number | null = null;
  jql = '';

  issues: JiraIssue[] = [];
  truncated = false;
  max = 100;
  selected = new Set<string>();

  ngOnInit(): void {
    this.apollo.query<{ jiraConnection: JiraConnectionStatus }>({ query: GET_JIRA_CONNECTION, fetchPolicy: 'no-cache' })
      .subscribe({
        next: ({ data }) => {
          this.status = data?.jiraConnection ?? null;
          this.loading = false;
          if (this.status?.connected) this.loadBoards();
        },
        // An older gateway without the Jira fields: the tab just says it is not available.
        error: () => { this.status = null; this.loading = false; }
      });
  }

  /** Off to Atlassian's consent page; the callback page brings the host back to this game. */
  connect(): void {
    this.connecting = true;
    const returnPath = this.document.location.pathname;
    this.apollo.query<{ jiraAuthorizeUrl: { url?: string } & Partial<AppError> }>({
      query: GET_JIRA_AUTHORIZE_URL, variables: { returnPath }, fetchPolicy: 'no-cache'
    }).subscribe({
      next: ({ data }) => {
        const result = data?.jiraAuthorizeUrl;
        if (result?.url) {
          this.document.location.href = result.url;
          return;
        }
        this.connecting = false;
        this.error = result?.message ?? '';
      },
      error: () => { this.connecting = false; }
    });
  }

  disconnect(): void {
    this.apollo.mutate({ mutation: DISCONNECT_JIRA }).subscribe(() => {
      this.status = { ...this.status!, connected: false, siteName: null };
      this.issues = [];
      this.selected.clear();
    });
  }

  onBoardChange(): void {
    this.sprintId = null;
    this.sprints = [];
    if (!this.boardId) return;
    this.apollo.query<{ jiraSprints: { items?: JiraSprint[] } & Partial<AppError> }>({
      query: GET_JIRA_SPRINTS, variables: { boardId: this.boardId }, fetchPolicy: 'no-cache'
    }).subscribe(({ data }) => {
      const result = data?.jiraSprints;
      this.sprints = result?.items ?? [];
      // The active sprint first: that is usually the one being planned.
      this.sprintId = this.sprints.find(sprint => sprint.state === 'active')?.id ?? this.sprints[0]?.id ?? null;
      this.error = result?.message ?? '';
    });
  }

  preview(): void {
    this.searching = true;
    this.error = '';
    const variables = this.source === 'sprint'
      ? { gameId: this.gameId, boardId: this.boardId, sprintId: this.sprintId }
      : { gameId: this.gameId, jql: this.jql.trim() };
    this.apollo.query<{ jiraIssues: { issues?: JiraIssue[]; truncated?: boolean; max?: number } & Partial<AppError> }>({
      query: GET_JIRA_ISSUES, variables, fetchPolicy: 'no-cache'
    }).subscribe({
      next: ({ data }) => {
        const result = data?.jiraIssues;
        this.searching = false;
        this.searched = true;
        this.issues = result?.issues ?? [];
        this.truncated = !!result?.truncated;
        this.max = result?.max ?? 100;
        this.selected = new Set(this.issues.filter(issue => !issue.alreadyInGame).map(issue => issue.key));
        this.error = result?.message ?? '';
      },
      error: () => { this.searching = false; }
    });
  }

  toggle(key: string): void {
    if (this.selected.has(key)) this.selected.delete(key);
    else this.selected.add(key);
  }

  import(): void {
    this.importing = true;
    this.apollo.mutate<{ importJiraIssues: { jiraImportSummary?: { imported: number; skipped: number }; errors?: AppError[] } }>({
      mutation: IMPORT_JIRA_ISSUES,
      variables: { input: { gameId: this.gameId, issueKeys: [...this.selected] } }
    }).subscribe({
      next: ({ data }) => {
        this.importing = false;
        const result = data?.importJiraIssues;
        if (result?.errors?.length) {
          this.error = result.errors[0].message;
          return;
        }
        this.imported.emit(result?.jiraImportSummary?.imported ?? 0);
      },
      error: () => { this.importing = false; }
    });
  }

  private loadBoards(): void {
    this.apollo.query<{ jiraBoards: { items?: JiraBoard[] } & Partial<AppError> }>({ query: GET_JIRA_BOARDS, fetchPolicy: 'no-cache' })
      .subscribe(({ data }) => {
        const result = data?.jiraBoards;
        this.boards = result?.items ?? [];
        if (result?.code === 'JiraReconnect') this.status = { ...this.status!, connected: false, needsReconnect: true };
        this.error = result?.code === 'JiraReconnect' ? '' : (result?.message ?? '');
        if (this.boards.length === 1) {
          this.boardId = this.boards[0].id;
          this.onBoardChange();
        }
      });
  }
}
