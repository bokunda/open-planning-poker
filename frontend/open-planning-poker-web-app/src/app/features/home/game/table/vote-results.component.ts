import { ChangeDetectionStrategy, Component, DoCheck, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiCollectionOfGamePlayer, Vote } from '../../../../graphql/graphql-gateway.service';
import { computeVoteStats, formatNumber, suggestedEstimate, VoteStats } from './vote-stats';

/**
 * Shown after the reveal: how the votes are distributed, average and median, and what the
 * team should do next (agree, discuss the outliers, or vote again).
 */
@Component({
  selector: 'app-vote-results',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.Default,
  template: `
    <section class="results" *ngIf="stats" aria-live="polite" aria-label="Voting results">
      <div class="results-head">
        <span class="chip" [ngClass]="'chip--' + stats.status">{{ statusLabel }}</span>
        <span class="stat">Average <b class="opp-tabular">{{ fmt(stats.average) }}</b></span>
        <span class="stat">Median <b class="opp-tabular">{{ fmt(stats.median) }}</b></span>
        <span class="stat">Votes <b class="opp-tabular">{{ totalVotes }}</b></span>
      </div>
      <div class="bars">
        <div class="bar-row" *ngFor="let c of stats.counts; let i = index">
          <span class="bar-label">{{ c.value }}</span>
          <span class="bar-track"><span class="bar-fill" [style.--w]="c.share * 100 + '%'" [style.--d]="i * 80 + 'ms'"></span></span>
          <span class="bar-count opp-tabular">{{ c.count }}</span>
        </div>
      </div>
      <p class="results-note">{{ note }}</p>

      <div class="final" *ngIf="isHost || finalEstimate">
        <span class="final-label" id="final-label">Final estimate</span>
        <div class="final-cards" *ngIf="isHost; else finalReadOnly" role="radiogroup" aria-labelledby="final-label">
          <button type="button" class="final-card opp-tabular" *ngFor="let card of finalOptions"
            role="radio" [attr.aria-checked]="card === finalEstimate"
            [class.final-card--selected]="card === finalEstimate"
            [class.final-card--suggested]="card === suggested && !finalEstimate"
            [attr.title]="card === suggested ? 'Most common card' : null"
            (click)="finalEstimateChange.emit(card === finalEstimate ? null : card)">{{ card }}</button>
        </div>
        <ng-template #finalReadOnly><b class="final-value opp-tabular">{{ finalEstimate }}</b></ng-template>
        <span class="final-hint" *ngIf="isHost && !finalEstimate">Pick the card the team agreed on. It goes into the ticket list, the CSV and the report.</span>
      </div>
    </section>
  `,
  styles: [`
    :host { display: block; width: 100%; max-width: 560px; margin: 0 auto; }
    .results {
      display: grid;
      gap: var(--opp-space-4);
      padding: var(--opp-space-5);
      background: var(--opp-surface);
      border: 1px solid var(--opp-border);
      border-radius: var(--opp-radius-lg);
      box-shadow: var(--opp-shadow-sm);
      animation: results-in var(--opp-duration-slow) var(--opp-ease-out) both;
    }
    @keyframes results-in { from { opacity: 0; translate: 0 8px; } }
    .results-head { display: flex; flex-wrap: wrap; align-items: center; gap: var(--opp-space-2) var(--opp-space-5); }
    .chip { font-size: var(--opp-font-size-sm); font-weight: 600; padding: 4px 12px; border-radius: var(--opp-radius-full); }
    .chip--consensus, .chip--close { background: color-mix(in srgb, var(--opp-consensus) 16%, transparent); color: var(--opp-consensus); }
    .chip--spread { background: color-mix(in srgb, var(--opp-spread) 18%, transparent); color: var(--opp-spread); }
    .chip--none { background: var(--opp-surface-hover); color: var(--opp-text-secondary); }
    .stat { color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); }
    .stat b { color: var(--opp-text); font-family: var(--opp-font-display); font-size: var(--opp-font-size-lg); margin-left: 4px; }
    .bars { display: grid; gap: var(--opp-space-2); }
    .bar-row { display: grid; grid-template-columns: 40px minmax(0, 1fr) 28px; align-items: center; gap: var(--opp-space-3); }
    .bar-label { font-family: var(--opp-font-display); font-weight: 700; text-align: right; }
    .bar-track { height: 12px; border-radius: var(--opp-radius-full); background: var(--opp-primary-50); overflow: hidden; }
    .bar-fill {
      display: block; height: 100%; width: var(--w); border-radius: inherit; background: var(--opp-primary);
      transform-origin: left; animation: grow var(--opp-duration-slow) var(--opp-ease-out) both; animation-delay: var(--d);
    }
    @keyframes grow { from { transform: scaleX(0); } }
    .bar-count { color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); }
    .results-note { margin: 0; color: var(--opp-text-secondary); }
    .final { display: grid; gap: var(--opp-space-2); padding-top: var(--opp-space-3); border-top: 1px solid var(--opp-border-light, var(--opp-border)); }
    .final-label { font-size: var(--opp-font-size-sm); font-weight: 700; color: var(--opp-text); }
    .final-cards { display: flex; flex-wrap: wrap; gap: var(--opp-space-2); }
    .final-card {
      min-width: 40px; height: 40px; padding: 0 10px; cursor: pointer;
      border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md);
      background: var(--opp-surface); color: var(--opp-text);
      font-family: var(--opp-font-display); font-weight: 800; font-size: var(--opp-font-size-md, 1rem);
      transition: border-color var(--opp-transition-fast), background var(--opp-transition-fast);
    }
    .final-card:hover { border-color: var(--opp-primary-300, var(--opp-primary)); }
    .final-card:focus-visible { outline: 3px solid var(--opp-focus-ring, var(--opp-primary)); outline-offset: 2px; }
    .final-card--suggested { border-style: dashed; border-color: var(--opp-primary); }
    .final-card--selected { background: var(--opp-primary); border-color: var(--opp-primary); color: var(--opp-text-on-primary, #fff); }
    .final-value { font-family: var(--opp-font-display); font-size: var(--opp-font-size-2xl); color: var(--opp-consensus); }
    .final-hint { font-size: var(--opp-font-size-xs); color: var(--opp-text-muted); }
  `],
})
export class VoteResultsComponent implements DoCheck {
  @Input() votes: Vote[] = [];
  @Input() players: ApiCollectionOfGamePlayer | undefined;
  @Input() deck: string[] = [];
  /** Host and co-hosts pick the final estimate; others see it once it is set. */
  @Input() isHost = false;
  @Input() finalEstimate: string | null | undefined = null;
  @Output() finalEstimateChange = new EventEmitter<string | null>();

  /** The deck's cards plus any voted value not in the deck. */
  finalOptions: string[] = [];
  suggested: string | null = null;

  stats: VoteStats | null = null;
  totalVotes = 0;
  statusLabel = '';
  note = '';
  readonly fmt = formatNumber;
  private signature = '';

  ngDoCheck(): void {
    const signature = this.votes.map(v => `${v.playerId}:${v.value}`).join('|') + '#' + this.deck.join(',')
      + '#' + (this.players?.items ?? []).map(p => `${p.id}:${p.name}`).join('|');
    if (signature === this.signature) return;
    this.signature = signature;

    const cast = this.votes.filter(v => !!v.value);
    this.totalVotes = cast.length;
    this.suggested = suggestedEstimate(cast);
    this.finalOptions = [...this.deck.filter(card => card.trim()), ...cast.map(v => v.value).filter(v => !this.deck.includes(v))]
      .filter((card, i, all) => all.indexOf(card) === i);
    this.stats = cast.length ? computeVoteStats(cast, this.deck) : null;
    if (!this.stats) return;

    const s = this.stats;
    switch (s.status) {
      case 'consensus':
        this.statusLabel = 'Consensus';
        this.note = 'Everyone agrees. Record the estimate and move to the next ticket.';
        break;
      case 'close':
        this.statusLabel = 'Close to consensus';
        this.note = 'The votes are one card apart. Pick the higher value or discuss it briefly.';
        break;
      case 'spread':
        this.statusLabel = 'Wide spread';
        this.note = `Ask ${this.names(s.lowPlayerIds)} (${formatNumber(s.min)}) and ${this.names(s.highPlayerIds)} (${formatNumber(s.max)}) to explain, then vote again.`;
        break;
      default:
        this.statusLabel = 'No numeric votes';
        this.note = 'Nobody picked a number yet. Discuss the ticket and vote again.';
    }
  }

  private names(ids: string[]): string {
    const items = this.players?.items ?? [];
    const names = ids.map(id => items.find(p => p.id === id)?.name ?? 'a player');
    return names.length <= 2 ? names.join(' and ') : `${names.slice(0, -1).join(', ')} and ${names[names.length - 1]}`;
  }
}
