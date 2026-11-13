import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, PLATFORM_ID, afterNextRender, inject } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ApiCollectionOfGamePlayer, Vote } from '../../../../graphql/graphql-gateway.service';
import { PokerTableComponent } from '../table/poker-table.component';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

const DECK = ['0', '0.5', '1', '2', '3', '5', '8', '13'];
const PLAYERS = [
  { id: 'you', name: 'You' },
  { id: 'ana', name: 'Ana' },
  { id: 'marko', name: 'Marko' },
  { id: 'jelena', name: 'Jelena' },
  { id: 'stefan', name: 'Stefan' },
];
/** Two rounds that tell the story: a wide spread, then the team converges. */
const ROUNDS: Record<string, string>[] = [
  { ana: '5', marko: '13', jelena: '5', stefan: '3', you: '5' },
  { ana: '5', marko: '8', jelena: '5', stefan: '5', you: '5' },
];

/**
 * Decorative table on the landing page that plays rounds on its own: players vote one by one,
 * the cards are revealed, then a new round starts. Uses the same table component as a real game.
 * Shows a finished round without motion for reduced motion and on the server.
 */
@Component({
  selector: 'app-demo-table',
  standalone: true,
  imports: [TranslocoPipe, CommonModule, PokerTableComponent],
  changeDetection: ChangeDetectionStrategy.Default,
  template: `
    <div class="demo" aria-hidden="true">
      <app-poker-table
        [players]="players"
        [votes]="votes"
        [votesRevealed]="revealed"
        currentUserId="you"
        [deck]="deck">
      </app-poker-table>
      <p class="demo-caption">{{ caption | transloco }}</p>
    </div>
  `,
  styles: [`
    :host { display: block; width: 100%; }
    .demo { display: grid; gap: var(--opp-space-2); justify-items: center; }
    .demo-caption { margin: 0; min-height: 1.5em; color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); text-align: center; }
  `],
})
export class DemoTableComponent {
  readonly deck = DECK;
  private readonly you = inject(TranslocoService).translate('common.you');
  readonly players: ApiCollectionOfGamePlayer = {
    items: PLAYERS.map(p => (p.id === 'you' ? { ...p, name: this.you } : p)),
    totalCount: PLAYERS.length,
  } as ApiCollectionOfGamePlayer;
  votes: Vote[] = [];
  revealed = false;
  caption = 'demo.secret';

  private readonly cdr = inject(ChangeDetectorRef);
  private readonly timers: ReturnType<typeof setTimeout>[] = [];
  private round = 0;

  constructor() {
    const isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    inject(DestroyRef).onDestroy(() => this.timers.forEach(clearTimeout));
    if (!isBrowser) {
      this.showFinished();
      return;
    }
    afterNextRender(() => {
      if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        this.showFinished();
      } else {
        this.playRound();
      }
      this.cdr.detectChanges();
    });
  }

  private showFinished(): void {
    this.votes = this.votesFor(ROUNDS[1], PLAYERS.map(p => p.id));
    this.revealed = true;
    this.caption = 'demo.reveal';
  }

  private playRound(): void {
    const plan = ROUNDS[this.round % ROUNDS.length];
    this.votes = [];
    this.revealed = false;
    this.caption = 'demo.secret';
    const order = ['stefan', 'ana', 'you', 'jelena', 'marko'];
    order.forEach((id, i) => this.later(900 + i * 650, () => {
      this.votes = this.votesFor(plan, order.slice(0, i + 1));
    }));
    const revealAt = 900 + order.length * 650 + 700;
    this.later(revealAt, () => {
      this.revealed = true;
      this.caption = this.round % ROUNDS.length === 0
        ? 'demo.spread'
        : 'demo.agree';
    });
    this.later(revealAt + 4200, () => {
      this.round++;
      this.playRound();
    });
  }

  private votesFor(plan: Record<string, string>, ids: string[]): Vote[] {
    return ids.map(id => ({ id: `demo-${id}`, playerId: id, value: plan[id] } as Vote));
  }

  private later(ms: number, fn: () => void): void {
    this.timers.push(setTimeout(() => {
      fn();
      this.cdr.detectChanges();
    }, ms));
  }
}
