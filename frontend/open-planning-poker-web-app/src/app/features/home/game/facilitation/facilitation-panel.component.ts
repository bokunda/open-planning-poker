import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, EventEmitter, Input, OnChanges, OnDestroy, Output, PLATFORM_ID, SimpleChanges, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Apollo } from 'apollo-angular';
import type { DocumentNode } from 'graphql';
import { Subscription } from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ApiCollectionOfGamePlayer, Vote } from '../../../../graphql/graphql-gateway.service';
import { computeVoteStats } from '../table/vote-stats';
import { ON_GAME_CHANGED } from '../gql/room.graphql';
import { SET_CONFIDENCE, SET_TICKET_NEEDS_INFO, SET_VOTE_NOTE, TICKET_FACILITATION } from '../gql/facilitation.graphql';
import { DEFAULT_FACILITATION, FacilitationSettings, GameChanged, LOW_CONFIDENCE, TicketFacilitation } from '../room-types';
import { OPP_SNACKBAR_DURATION_DEFAULT } from '../../../../shared/constants';

/** "?" cards (the default deck uses ❓). */
export const isUnsureCard = (value: string | null | undefined) => !!value && (value.includes('?') || value.includes('❓'));
/** Break cards (the default deck uses ⏸️). */
export const isBreakCard = (value: string | null | undefined) => !!value && (value.includes('☕') || value.includes('⏸'));

/** Most of these cards and the room is unsure or tired. */
const HALF = 0.5;
export const NOTE_MAX = 200;

/**
 * Shown under the results after the reveal (OPP-114): a discussion timer, notes from the lowest and
 * highest voters, the "?" and break cards, and a confidence check once the final estimate is chosen.
 */
@Component({
  selector: 'app-facilitation-panel',
  standalone: true,
  imports: [TranslocoPipe, FormsModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (hasContent) {
      <section class="facilitation" [attr.aria-label]="'facilitation.title' | transloco">
        @if (options.discussionMinutes > 0 && remaining !== null) {
          <div class="row discussion" [class.discussion--over]="remaining <= 0" role="timer" aria-live="off">
            <span class="label">{{ 'facilitation.discussion' | transloco }}</span>
            <b class="clock opp-tabular">{{ clock }}</b>
            @if (remaining <= 0) {
              <span class="over" role="status">{{ 'facilitation.timeUp' | transloco }}</span>
              @if (isHost) {
                <button mat-stroked-button type="button" (click)="voteAgain.emit()">{{ 'facilitation.voteAgain' | transloco }}</button>
              }
            }
          </div>
        }

        @if (unsureCount || coffeeCount) {
          <div class="row special" role="status">
            @if (unsureCount) {
              <span class="pill">❓ × {{ unsureCount }}</span>
            }
            @if (coffeeCount) {
              <span class="pill">⏸️ × {{ coffeeCount }}</span>
            }
            @if (mostlyUnsure) {
              <span class="hint">{{ 'facilitation.needsInfoHint' | transloco }}</span>
            }
            @if (mostlyCoffee && isHost) {
              <span class="hint">{{ 'facilitation.breakHint' | transloco }}</span>
            }
          </div>
        }

        @if (isHost && (mostlyUnsure || info?.needsInfo)) {
          <label class="row check">
            <input type="checkbox" [checked]="!!info?.needsInfo" (change)="toggleNeedsInfo()">
            {{ 'facilitation.markNeedsInfo' | transloco }}
          </label>
        } @else if (info?.needsInfo) {
          <p class="row flag">{{ 'facilitation.needsInfo' | transloco }}</p>
        }

        @if (options.outlierNotes && (iAmOutlier || info?.notes?.length)) {
          <div class="notes">
            @if (iAmOutlier) {
              <form class="note-form" (ngSubmit)="saveNote()">
                <label class="label" for="vote-note">{{ 'facilitation.tellWhy' | transloco }}</label>
                <div class="note-input">
                  <input id="vote-note" name="note" [(ngModel)]="noteDraft" [maxLength]="noteMax"
                    [placeholder]="'facilitation.notePlaceholder' | transloco" autocomplete="off">
                  <button mat-flat-button color="primary" type="submit" [disabled]="saving">{{ 'facilitation.saveNote' | transloco }}</button>
                </div>
              </form>
            }
            @for (n of info?.notes ?? []; track n.playerId) {
              <p class="note"><b>{{ name(n.playerId) }} ({{ voteOf(n.playerId) }}):</b> {{ n.note }}</p>
            }
          </div>
        }

        @if (options.confidenceCheck && finalEstimate) {
          <div class="row confidence">
            <span class="label" id="confidence-label">{{ 'facilitation.confidence' | transloco }}</span>
            @if (canRate) {
              <div class="scale" role="radiogroup" aria-labelledby="confidence-label">
                @for (level of levels; track level) {
                  <button type="button" class="level opp-tabular" role="radio"
                    [attr.aria-checked]="level === info?.myConfidence" [class.level--on]="level === info?.myConfidence"
                    [attr.aria-label]="('facilitation.level' + level) | transloco" [attr.title]="('facilitation.level' + level) | transloco"
                    (click)="rate(level)">{{ level }}</button>
                }
              </div>
            }
            @if (info?.confidenceCount) {
              <span class="avg" [class.avg--low]="lowConfidence">
                {{ 'facilitation.confidenceAverage' | transloco: { avg: info!.confidenceAverage, count: info!.confidenceCount } }}
                @if (lowConfidence) { · {{ 'facilitation.lowConfidence' | transloco }} }
              </span>
            }
          </div>
        }
      </section>
    }
  `,
  styles: [`
    :host { display: block; width: 100%; max-width: 560px; margin: var(--opp-space-3) auto 0; }
    .facilitation { display: grid; gap: var(--opp-space-3); padding: var(--opp-space-4) var(--opp-space-5);
      background: var(--opp-surface); border: 1px solid var(--opp-border); border-radius: var(--opp-radius-lg); }
    .row { display: flex; flex-wrap: wrap; align-items: center; gap: var(--opp-space-2) var(--opp-space-3); margin: 0; }
    .label { font-size: var(--opp-font-size-sm); font-weight: 700; color: var(--opp-text); }
    .clock { font-family: var(--opp-font-display); font-size: var(--opp-font-size-lg); }
    .discussion--over .clock, .over { color: var(--opp-spread); }
    .over { font-size: var(--opp-font-size-sm); font-weight: 600; }
    .pill { font-weight: 700; padding: 2px 10px; border-radius: var(--opp-radius-full); background: var(--opp-surface-hover); }
    .hint { font-size: var(--opp-font-size-sm); color: var(--opp-text-secondary); }
    .check { font-size: var(--opp-font-size-sm); color: var(--opp-text-secondary); cursor: pointer; }
    .flag { font-size: var(--opp-font-size-sm); font-weight: 600; color: var(--opp-spread); }
    .notes { display: grid; gap: var(--opp-space-2); }
    .note-form { display: grid; gap: var(--opp-space-1); }
    .note-input { display: flex; gap: var(--opp-space-2); }
    .note-input input { flex: 1; min-width: 0; padding: 8px 10px; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md);
      background: var(--opp-surface); color: var(--opp-text); font: inherit; }
    .note-input input:focus-visible { outline: 3px solid var(--opp-focus-ring, var(--opp-primary)); outline-offset: 1px; }
    .note { margin: 0; font-size: var(--opp-font-size-sm); color: var(--opp-text-secondary); overflow-wrap: anywhere; }
    .note b { color: var(--opp-text); }
    .scale { display: flex; gap: var(--opp-space-1); }
    .level { width: 36px; height: 36px; cursor: pointer; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md);
      background: var(--opp-surface); color: var(--opp-text); font-family: var(--opp-font-display); font-weight: 800; }
    .level:focus-visible { outline: 3px solid var(--opp-focus-ring, var(--opp-primary)); outline-offset: 2px; }
    .level--on { background: var(--opp-primary); border-color: var(--opp-primary); color: var(--opp-text-on-primary, #fff); }
    .avg { font-size: var(--opp-font-size-sm); color: var(--opp-text-secondary); }
    .avg--low { color: var(--opp-spread); font-weight: 600; }
  `],
})
export class FacilitationPanelComponent implements OnChanges, OnDestroy {
  @Input() gameId = '';
  @Input() ticketId = '';
  @Input() votes: Vote[] = [];
  @Input() players: ApiCollectionOfGamePlayer | undefined;
  @Input() deck: string[] = [];
  @Input() currentUserId = '';
  @Input() isHost = false;
  @Input() isObserver = false;
  @Input() finalEstimate: string | null = null;
  @Input() options: FacilitationSettings = DEFAULT_FACILITATION;
  @Output() voteAgain = new EventEmitter<void>();

  info: TicketFacilitation | null = null;
  remaining: number | null = null;
  noteDraft = '';
  saving = false;
  readonly levels = [1, 2, 3, 4, 5];
  readonly noteMax = NOTE_MAX;

  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private tick: ReturnType<typeof setInterval> | undefined;
  private changes: Subscription | undefined;
  private load: Subscription | undefined;
  private lowIds: string[] = [];
  private highIds: string[] = [];

  get unsureCount(): number { return this.votes.filter(v => isUnsureCard(v.value)).length; }
  get coffeeCount(): number { return this.votes.filter(v => isBreakCard(v.value)).length; }
  private get castCount(): number { return this.votes.filter(v => !!v.value).length; }
  get mostlyUnsure(): boolean { return this.castCount > 0 && this.unsureCount / this.castCount >= HALF; }
  get mostlyCoffee(): boolean { return this.castCount > 0 && this.coffeeCount / this.castCount >= HALF; }

  get iAmOutlier(): boolean {
    return !this.isObserver && !!this.currentUserId && [...this.lowIds, ...this.highIds].includes(this.currentUserId);
  }

  get canRate(): boolean {
    return !this.isObserver && this.votes.some(v => v.playerId === this.currentUserId);
  }

  get lowConfidence(): boolean {
    return (this.info?.confidenceAverage ?? LOW_CONFIDENCE) < LOW_CONFIDENCE;
  }

  get hasContent(): boolean {
    return (this.options.discussionMinutes > 0 && this.remaining !== null) || !!this.unsureCount || !!this.coffeeCount
      || !!this.info?.needsInfo || (this.options.outlierNotes && (this.iAmOutlier || !!this.info?.notes?.length))
      || (this.options.confidenceCheck && !!this.finalEstimate);
  }

  get clock(): string {
    const s = Math.max(0, this.remaining ?? 0);
    return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['votes'] || changes['deck']) {
      const stats = computeVoteStats(this.votes, this.deck);
      this.lowIds = stats.lowPlayerIds;
      this.highIds = stats.highPlayerIds;
    }
    if (changes['gameId'] && this.gameId && this.isBrowser) this.listen();
    if (changes['ticketId'] || changes['finalEstimate']) this.fetch();
    else if (changes['options']) this.startClock();
  }

  ngOnDestroy(): void {
    clearInterval(this.tick);
    this.changes?.unsubscribe();
    this.load?.unsubscribe();
  }

  name(playerId: string): string {
    if (playerId === this.currentUserId) return this.i18n.translate('common.you');
    return this.players?.items?.find(p => p.id === playerId)?.name ?? this.i18n.translate('results.aPlayer');
  }

  voteOf(playerId: string): string {
    return this.votes.find(v => v.playerId === playerId)?.value ?? '';
  }

  saveNote(): void {
    this.saving = true;
    this.send(SET_VOTE_NOTE, 'setVoteNote', { ticketId: this.ticketId, note: this.noteDraft.trim() || null }, () => { this.saving = false; });
  }

  rate(level: number): void {
    const value = level === this.info?.myConfidence ? null : level;
    this.send(SET_CONFIDENCE, 'setConfidence', { ticketId: this.ticketId, value });
  }

  toggleNeedsInfo(): void {
    this.send(SET_TICKET_NEEDS_INFO, 'setTicketNeedsInfo', { ticketId: this.ticketId, needsInfo: !this.info?.needsInfo });
  }

  private fetch(): void {
    this.load?.unsubscribe();
    if (!this.ticketId || !this.isBrowser) return;
    this.load = this.apollo.query<{ ticketFacilitation: TicketFacilitation & { code?: string } }>({
      query: TICKET_FACILITATION, variables: { ticketId: this.ticketId }, fetchPolicy: 'network-only',
    }).subscribe({
      next: ({ data }) => {
        const info = data?.ticketFacilitation;
        if (info && !info.code) this.apply(info);
      },
    });
  }

  private apply(info: TicketFacilitation): void {
    this.info = info;
    const mine = info.notes.find(n => n.playerId === this.currentUserId)?.note;
    if (mine !== undefined && !this.saving) this.noteDraft = mine;
    this.startClock();
    this.cdr.markForCheck();
  }

  private startClock(): void {
    clearInterval(this.tick);
    const revealedAt = this.info?.revealedAt ? new Date(this.info.revealedAt).getTime() : null;
    if (!revealedAt || !this.options.discussionMinutes || this.finalEstimate) {
      this.remaining = null;
      return;
    }
    const end = revealedAt + this.options.discussionMinutes * 60_000;
    const update = () => {
      this.remaining = Math.round((end - Date.now()) / 1000);
      if (this.remaining <= 0) clearInterval(this.tick);
      this.cdr.markForCheck();
    };
    update();
    this.tick = setInterval(update, 1000);
  }

  /** Notes and ratings of others arrive as "tickets changed" for this ticket. */
  private listen(): void {
    this.changes?.unsubscribe();
    this.changes = this.apollo.subscribe<{ onGameChanged: GameChanged }>({ query: ON_GAME_CHANGED, variables: { gameId: this.gameId } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ data }) => {
        const change = data?.onGameChanged;
        if (change?.kind === 'TICKETS' && change.ticketId === this.ticketId) this.fetch();
        if (change?.kind === 'GAME') this.startClock();
      });
  }

  private send(mutation: DocumentNode, field: string, input: Record<string, unknown>, done?: () => void): void {
    this.apollo.mutate<Record<string, { ticketFacilitationInfo?: TicketFacilitation; errors?: { message: string }[] }>>({
      mutation, variables: { input },
    }).subscribe({
      next: ({ data }) => {
        done?.();
        const payload = data?.[field];
        if (payload?.errors?.length) {
          this.notify(payload.errors[0].message);
        } else if (payload?.ticketFacilitationInfo) {
          this.apply(payload.ticketFacilitationInfo);
        }
      },
      error: () => { done?.(); this.notify(this.i18n.translate('common.tryAgainNetwork')); },
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, this.i18n.translate('common.close'), { duration: OPP_SNACKBAR_DURATION_DEFAULT * 2 });
    this.cdr.markForCheck();
  }
}
