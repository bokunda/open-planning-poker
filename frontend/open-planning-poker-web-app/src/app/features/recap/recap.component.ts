import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Apollo } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import { OppDatePipe } from '../../i18n/opp-date.pipe';
import { PokerCardComponent } from '../../shared/poker-card/poker-card.component';
import { trackEvent } from '../../shared/analytics';
import { environment } from '../../../environments/environment';
import { GAME_RECAP, GameRecap, RECAP_ORIGIN } from './recap.graphql';

/**
 * The public recap of a session (OPP-170): rendered on the server so link previews and slow phones get it at once.
 * Read-only, no sign-in, and kept out of search results.
 */
@Component({
  selector: 'app-recap',
  standalone: true,
  imports: [TranslocoPipe, OppDatePipe, RouterLink, MatButtonModule, MatIconModule, PokerCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The server render carries the link preview tags; the browser loads the recap again and renders it fresh, so the
  // loading state never has to match the server HTML.
  host: { ngSkipHydration: 'true' },
  templateUrl: './recap.component.html',
  styleUrl: './recap.component.scss',
})
export class RecapComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly meta = inject(Meta);
  private readonly title = inject(Title);
  private readonly i18n = inject(TranslocoService);

  recap: GameRecap | null = null;
  notFound = false;
  loading = true;
  token = '';

  get consensusPercent(): number | null {
    return this.recap?.consensusRate == null ? null : Math.round(this.recap.consensusRate * 100);
  }

  get points(): string {
    const p = this.recap?.totalPoints ?? 0;
    return Number.isInteger(p) ? String(p) : p.toFixed(1);
  }

  async ngOnInit(): Promise<void> {
    this.token = this.route.snapshot.paramMap.get('token') ?? '';
    try {
      const result = await firstValueFrom(this.apollo.query<{ gameRecap: GameRecap | { code: string } }>({
        query: GAME_RECAP, variables: { token: this.token }, fetchPolicy: 'network-only',
      }));
      const value = result.data?.gameRecap;
      if (value && 'gameName' in value) {
        this.recap = value as GameRecap;
        this.applyMeta(value);
      } else {
        this.notFound = true;
      }
    } catch {
      this.notFound = true;
    }
    this.loading = false;
    this.cdr.markForCheck();
  }

  spreadKey(spread: string): string {
    return `recap.spread.${spread}`;
  }

  onStartRoom(): void {
    trackEvent('recap_start_room');
  }

  /** Link preview: room name and numbers, with the card image from the game engine (OPP-171). */
  private applyMeta(recap: GameRecap): void {
    const pageTitle = this.i18n.translate('recap.metaTitle', { name: recap.gameName });
    const description = this.i18n.translate('recap.metaDescription', {
      count: recap.ticketsEstimated,
      points: this.points,
      consensus: this.consensusPercent ?? 0,
    });
    const url = `${RECAP_ORIGIN}/recap/${this.token}`;
    const image = `${environment.recapImageBase}/recap/${this.token}/card.png`;

    this.title.setTitle(pageTitle);
    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({ property: 'og:title', content: pageTitle });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.meta.updateTag({ property: 'og:image', content: image });
    this.meta.updateTag({ property: 'og:image:alt', content: pageTitle });
    this.meta.updateTag({ name: 'twitter:title', content: pageTitle });
    this.meta.updateTag({ name: 'twitter:description', content: description });
    this.meta.updateTag({ name: 'twitter:image', content: image });
    this.meta.updateTag({ name: 'twitter:image:alt', content: pageTitle });
    this.meta.updateTag({ name: 'twitter:url', content: url });
  }
}
