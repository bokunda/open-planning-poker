import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Apollo, gql } from 'apollo-angular';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OPP_SNACKBAR_DURATION_DEFAULT } from '../../../../shared/constants';
import { looksTooBig } from './ai-helpers';

const ERRORS = `errors { ... on ApplicationError { code message } }`;

export const AI_ASSISTANT = gql`
  query AiAssistant($gameId: UUID!) {
    aiAssistant(gameId: $gameId) { ... on AiAssistantStatus { available enabled } ... on ApplicationError { code } }
  }`;
export const TICKET_AI = gql`
  query TicketAi($id: UUID!) { ticket(id: $id) { ... on Ticket { id ai { clarity summary } } } }`;
export const SET_AI_ASSISTANT = gql`
  mutation SetAiAssistant($input: SetAiAssistantInput!) { setAiAssistant(input: $input) { aiAssistantStatus { available enabled } ${ERRORS} } }`;
export const CHECK_TICKET_CLARITY = gql`
  mutation CheckTicketClarity($input: CheckTicketClarityInput!) { checkTicketClarity(input: $input) { ticketClarity { points } ${ERRORS} } }`;
export const DISMISS_TICKET_CLARITY = gql`
  mutation DismissTicketClarity($input: DismissTicketClarityInput!) { dismissTicketClarity(input: $input) { boolean ${ERRORS} } }`;
export const SUGGEST_TICKET_SPLIT = gql`
  mutation SuggestTicketSplit($input: SuggestTicketSplitInput!) { suggestTicketSplit(input: $input) { ticketSplitIdeas { items { title line } } ${ERRORS} } }`;
export const SUMMARIZE_TICKET_DISCUSSION = gql`
  mutation SummarizeTicketDiscussion($input: SummarizeTicketDiscussionInput!) { summarizeTicketDiscussion(input: $input) { ticketDiscussionSummary { summary } ${ERRORS} } }`;
export const SET_TICKET_DISCUSSION_SUMMARY = gql`
  mutation SetTicketDiscussionSummary($input: SetTicketDiscussionSummaryInput!) { setTicketDiscussionSummary(input: $input) { boolean ${ERRORS} } }`;

type Payload = { errors?: { code: string; message: string }[] | null } | null | undefined;

/**
 * Optional AI help around the estimate (OPP-150 to OPP-152): a clarity check before voting that everyone sees, ideas to
 * split a ticket the team found too big, and a one-sentence summary of the discussion for the tracker comment. It never
 * suggests a card.
 */
@Component({
  selector: 'app-ai-assist',
  standalone: true,
  imports: [TranslocoPipe, FormsModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (available && ticketId) {
      <section class="ai" [attr.aria-label]="'ai.title' | transloco">
        @if (clarity.length) {
          <div class="panel" role="note">
            <p class="head"><mat-icon aria-hidden="true">lightbulb</mat-icon> <strong>{{ 'ai.clarityTitle' | transloco }}</strong></p>
            <ul>@for (p of clarity; track p) { <li>{{ p }}</li> }</ul>
            @if (isHost) { <button mat-button type="button" (click)="dismiss()">{{ 'ai.dismiss' | transloco }}</button> }
          </div>
        }

        @if (isHost) {
          @if (!enabled) {
            <button mat-button type="button" class="enable" (click)="setEnabled(true)">
              <mat-icon>auto_awesome</mat-icon> {{ 'ai.enable' | transloco }}
            </button>
          } @else {
            <div class="row">
              @if (!votesRevealed) {
                <button mat-stroked-button type="button" (click)="check()" [disabled]="busy">
                  <mat-icon>fact_check</mat-icon> {{ 'ai.check' | transloco }}
                </button>
              }
              @if (votesRevealed && tooBig) {
                <button mat-stroked-button type="button" (click)="split()" [disabled]="busy">
                  <mat-icon>call_split</mat-icon> {{ 'ai.split' | transloco }}
                </button>
              }
              @if (votesRevealed && finalEstimate) {
                <button mat-stroked-button type="button" (click)="summarize()" [disabled]="busy">
                  <mat-icon>short_text</mat-icon> {{ 'ai.summarize' | transloco }}
                </button>
              }
              <button mat-button type="button" class="off" (click)="setEnabled(false)">{{ 'ai.disable' | transloco }}</button>
            </div>
            @if (busy) { <p class="muted" aria-live="polite">{{ 'ai.working' | transloco }}</p> }

            @if (ideas.length) {
              <div class="panel">
                <p class="head"><strong>{{ 'ai.ideasTitle' | transloco }}</strong></p>
                <ul class="ideas">
                  @for (i of ideas; track i.title) {
                    <li>
                      <span><strong>{{ i.title }}</strong> — {{ i.line }}</span>
                      <button mat-button type="button" (click)="addIdea(i)" [disabled]="added.has(i.title)">
                        {{ (added.has(i.title) ? 'ai.added' : 'ai.addTicket') | transloco }}
                      </button>
                    </li>
                  }
                </ul>
              </div>
            }

            @if (summaryDraft !== null) {
              <form class="panel" (ngSubmit)="saveSummary()">
                <label for="ai-summary" class="head"><strong>{{ 'ai.summaryLabel' | transloco }}</strong></label>
                <textarea id="ai-summary" name="summary" rows="2" maxlength="300" [(ngModel)]="summaryDraft"></textarea>
                <div class="row">
                  <button mat-flat-button color="primary" type="submit">{{ 'ai.saveSummary' | transloco }}</button>
                  <button mat-button type="button" (click)="summaryDraft = null">{{ 'ai.cancel' | transloco }}</button>
                </div>
                <p class="muted">{{ 'ai.summaryHint' | transloco }}</p>
              </form>
            } @else if (summary) {
              <p class="muted"><strong>{{ 'ai.summaryLabel' | transloco }}:</strong> {{ summary }}
                <button mat-button type="button" (click)="summaryDraft = summary">{{ 'ai.edit' | transloco }}</button></p>
            }
          }
        } @else if (summary) {
          <p class="muted"><strong>{{ 'ai.summaryLabel' | transloco }}:</strong> {{ summary }}</p>
        }
      </section>
    }
  `,
  styles: [`
    :host { display: block; }
    .ai { display: grid; gap: var(--opp-space-2); margin: var(--opp-space-2) 0; }
    .panel { display: grid; gap: var(--opp-space-2); padding: var(--opp-space-3) var(--opp-space-4); border: 1px solid var(--opp-border);
      border-left: 3px solid var(--opp-primary); border-radius: var(--opp-radius-md); background: var(--opp-surface); }
    .panel ul { margin: 0; padding-left: 1.2em; display: grid; gap: 4px; }
    .head { display: flex; align-items: center; gap: var(--opp-space-2); margin: 0; }
    .head mat-icon { color: var(--opp-primary); }
    .row { display: flex; flex-wrap: wrap; align-items: center; gap: var(--opp-space-2); }
    .ideas { list-style: none; padding: 0 !important; }
    .ideas li { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: var(--opp-space-2); }
    .muted { margin: 0; color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); }
    .off { color: var(--opp-text-secondary); }
    textarea { font: inherit; color: var(--opp-text); background: var(--opp-bg); border: 1px solid var(--opp-border);
      border-radius: var(--opp-radius-sm); padding: var(--opp-space-2); resize: vertical; }
  `],
})
export class AiAssistComponent implements OnChanges {
  @Input() gameId = '';
  @Input() ticketId: string | null = null;
  @Input() isHost = false;
  @Input() votesRevealed = false;
  @Input() finalEstimate: string | null = null;
  @Input() voteValues: string[] = [];
  @Input() deck: string[] = [];
  /** Changes whenever the room's tickets or options changed, so the panel reloads. */
  @Input() version = 0;
  @Output() addTicket = new EventEmitter<{ name: string; description: string }>();

  private readonly apollo = inject(Apollo);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);
  private readonly cdr = inject(ChangeDetectorRef);

  available = false;
  enabled = false;
  clarity: string[] = [];
  summary: string | null = null;
  summaryDraft: string | null = null;
  ideas: { title: string; line: string }[] = [];
  added = new Set<string>();
  busy = false;

  get tooBig(): boolean { return looksTooBig(this.voteValues, this.deck); }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['gameId'] || changes['version']) && this.gameId) this.loadStatus();
    if (changes['ticketId']) { this.ideas = []; this.added.clear(); this.summaryDraft = null; }
    if ((changes['ticketId'] || changes['version'] || changes['gameId']) && this.ticketId) this.loadTicket();
  }

  private loadStatus(): void {
    this.apollo.query<{ aiAssistant: { available?: boolean; enabled?: boolean } }>({ query: AI_ASSISTANT, variables: { gameId: this.gameId }, fetchPolicy: 'no-cache' })
      .subscribe(({ data }) => {
        this.available = !!data?.aiAssistant?.available;
        this.enabled = !!data?.aiAssistant?.enabled;
        this.cdr.markForCheck();
      });
  }

  private loadTicket(): void {
    if (!this.ticketId) return;
    this.apollo.query<{ ticket: { ai?: { clarity: string[]; summary: string | null } } }>({ query: TICKET_AI, variables: { id: this.ticketId }, fetchPolicy: 'no-cache' })
      .subscribe(({ data }) => {
        this.clarity = data?.ticket?.ai?.clarity ?? [];
        this.summary = data?.ticket?.ai?.summary ?? null;
        this.cdr.markForCheck();
      });
  }

  setEnabled(enabled: boolean): void {
    this.run<{ aiAssistantStatus?: { enabled: boolean } }>(SET_AI_ASSISTANT, 'setAiAssistant', { gameId: this.gameId, enabled }, r => {
      this.enabled = !!r?.aiAssistantStatus?.enabled;
    });
  }

  check(): void {
    this.run<{ ticketClarity?: { points: string[] } }>(CHECK_TICKET_CLARITY, 'checkTicketClarity', { ticketId: this.ticketId }, r => {
      this.clarity = r?.ticketClarity?.points ?? [];
      if (!this.clarity.length) this.notify(this.i18n.translate('ai.clear'));
    });
  }

  dismiss(): void {
    this.run(DISMISS_TICKET_CLARITY, 'dismissTicketClarity', { ticketId: this.ticketId }, () => { this.clarity = []; });
  }

  split(): void {
    this.run<{ ticketSplitIdeas?: { items: { title: string; line: string }[] } }>(SUGGEST_TICKET_SPLIT, 'suggestTicketSplit', { ticketId: this.ticketId }, r => {
      this.ideas = r?.ticketSplitIdeas?.items ?? [];
    });
  }

  addIdea(idea: { title: string; line: string }): void {
    this.added.add(idea.title);
    this.addTicket.emit({ name: idea.title, description: idea.line });
  }

  summarize(): void {
    this.run<{ ticketDiscussionSummary?: { summary: string } }>(SUMMARIZE_TICKET_DISCUSSION, 'summarizeTicketDiscussion', { ticketId: this.ticketId }, r => {
      this.summaryDraft = r?.ticketDiscussionSummary?.summary ?? '';
    });
  }

  saveSummary(): void {
    const summary = (this.summaryDraft ?? '').trim();
    this.run(SET_TICKET_DISCUSSION_SUMMARY, 'setTicketDiscussionSummary', { ticketId: this.ticketId, summary: summary || null }, () => {
      this.summary = summary || null;
      this.summaryDraft = null;
    });
  }

  private run<T extends object>(mutation: ReturnType<typeof gql>, field: string, input: Record<string, unknown>, done: (r: T | undefined) => void): void {
    this.busy = true;
    this.apollo.mutate<Record<string, Payload & T>>({ mutation, variables: { input } }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        const result = data?.[field];
        if (result?.errors?.length) this.notify(result.errors[0].message);
        else done(result as T | undefined);
        this.cdr.markForCheck();
      },
      error: () => { this.busy = false; this.notify(this.i18n.translate('common.tryAgainNetwork')); this.cdr.markForCheck(); },
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, undefined, { duration: OPP_SNACKBAR_DURATION_DEFAULT });
  }
}
