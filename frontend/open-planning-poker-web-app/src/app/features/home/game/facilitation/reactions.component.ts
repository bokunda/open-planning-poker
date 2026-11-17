import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, OnDestroy, PLATFORM_ID, SimpleChanges, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Apollo } from 'apollo-angular';
import { Subscription } from 'rxjs';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ApiCollectionOfGamePlayer } from '../../../../graphql/graphql-gateway.service';
import { ON_REACTION, SEND_REACTION } from '../gql/facilitation.graphql';
import { REACTIONS, Reaction } from '../room-types';

interface Floating { id: number; emoji: string; name: string; left: number; }

/** Keep the screen calm: at most this many reactions float at once. */
const MAX_FLOATING = 12;
const FLOAT_MS = 2600;

/**
 * Emoji reactions (OPP-125): a small bar to send one, and reactions of the room floating up for a
 * moment. Nothing is stored; with "reduce motion" they fade in place instead of floating.
 */
@Component({
  selector: 'app-reactions',
  standalone: true,
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bar" role="group" [attr.aria-label]="'facilitation.reactions' | transloco">
      @for (emoji of reactions; track emoji) {
        <button type="button" class="emoji" (click)="send(emoji)" [attr.aria-label]="('facilitation.react' | transloco) + ' ' + emoji">{{ emoji }}</button>
      }
    </div>
    <div class="stage" aria-hidden="true">
      @for (f of floating; track f.id) {
        <span class="float" [style.left.%]="f.left"><span class="float-emoji">{{ f.emoji }}</span><span class="float-name">{{ f.name }}</span></span>
      }
    </div>
    <span class="sr" role="status">{{ lastAnnouncement }}</span>
  `,
  styles: [`
    :host { position: relative; display: flex; justify-content: center; }
    .bar { display: inline-flex; gap: 2px; padding: 2px; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-full); background: var(--opp-surface); }
    .emoji { width: 34px; height: 34px; border: 0; border-radius: 50%; background: transparent; cursor: pointer; font-size: 18px; line-height: 1; }
    .emoji:hover { background: var(--opp-surface-hover); }
    .emoji:focus-visible { outline: 3px solid var(--opp-focus-ring, var(--opp-primary)); outline-offset: 1px; }
    .stage { position: absolute; left: 0; right: 0; bottom: 100%; height: 220px; pointer-events: none; overflow: hidden; }
    .float { position: absolute; bottom: 0; display: grid; justify-items: center; animation: rise ${FLOAT_MS}ms ease-out forwards; }
    .float-emoji { font-size: 28px; }
    .float-name { font-size: 11px; color: var(--opp-text-secondary); background: var(--opp-surface); padding: 0 6px; border-radius: var(--opp-radius-full); white-space: nowrap; }
    @keyframes rise { from { transform: translateY(0); opacity: 0; } 15% { opacity: 1; } to { transform: translateY(-200px); opacity: 0; } }
    @media (prefers-reduced-motion: reduce) {
      .float { animation: fade ${FLOAT_MS}ms ease-out forwards; bottom: 40px; }
      @keyframes fade { from { opacity: 0; } 15% { opacity: 1; } to { opacity: 0; } }
    }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
  `],
})
export class ReactionsComponent implements OnChanges, OnDestroy {
  @Input() gameId = '';
  @Input() players: ApiCollectionOfGamePlayer | undefined;
  @Input() currentUserId = '';

  readonly reactions = REACTIONS;
  floating: Floating[] = [];
  lastAnnouncement = '';

  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly i18n = inject(TranslocoService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private sub: Subscription | undefined;
  private nextId = 0;
  private timers: ReturnType<typeof setTimeout>[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['gameId'] && this.gameId && this.isBrowser) {
      this.sub?.unsubscribe();
      this.sub = this.apollo.subscribe<{ onReaction: Reaction }>({ query: ON_REACTION, variables: { gameId: this.gameId } })
        .subscribe(({ data }) => { if (data?.onReaction) this.show(data.onReaction); });
    }
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    this.timers.forEach(clearTimeout);
  }

  send(emoji: string): void {
    this.apollo.mutate({ mutation: SEND_REACTION, variables: { input: { gameId: this.gameId, emoji } } }).subscribe({ error: () => undefined });
  }

  private show(reaction: Reaction): void {
    const name = reaction.playerId === this.currentUserId
      ? this.i18n.translate('common.you')
      : this.players?.items?.find(p => p.id === reaction.playerId)?.name ?? '';
    const item: Floating = { id: this.nextId++, emoji: reaction.emoji, name, left: 10 + Math.random() * 80 };
    this.floating = [...this.floating, item].slice(-MAX_FLOATING);
    this.lastAnnouncement = `${name} ${reaction.emoji}`;
    this.timers.push(setTimeout(() => {
      this.floating = this.floating.filter(f => f.id !== item.id);
      this.cdr.markForCheck();
    }, FLOAT_MS));
    this.cdr.markForCheck();
  }
}
