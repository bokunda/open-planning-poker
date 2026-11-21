import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, inject, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Apollo } from 'apollo-angular';
import { RETRO_FACTS, SprintFacts, SprintFactTicket } from './retro.graphql';

/**
 * "Sprint in numbers" (OPP-167): points, the velocity trend, cycle-time outliers and tickets the team was unsure about.
 * Loads when opened; any fact becomes a card with one click while the team still writes.
 */
@Component({
  selector: 'app-retro-facts',
  standalone: true,
  imports: [TranslocoPipe, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './retro-facts.component.html',
  styleUrl: './retro-facts.component.scss',
})
export class RetroFactsComponent implements OnChanges {
  @Input({ required: true }) retroId!: string;
  @Input() canWrite = false;
  @Output() addCard = new EventEmitter<string>();

  private readonly apollo = inject(Apollo);
  private readonly i18n = inject(TranslocoService);
  private readonly cdr = inject(ChangeDetectorRef);

  open = false;
  facts: SprintFacts | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['retroId'] && !changes['retroId'].firstChange) {
      this.facts = null;
      if (this.open) this.load();
    }
  }

  toggle(): void {
    this.open = !this.open;
    if (this.open && !this.facts) this.load();
  }

  private load(): void {
    this.apollo.query<{ retroFacts: SprintFacts }>({ query: RETRO_FACTS, variables: { retroId: this.retroId }, fetchPolicy: 'no-cache' })
      .subscribe(({ data }) => {
        this.facts = data?.retroFacts?.velocity ? data.retroFacts : { hasData: false } as SprintFacts;
        this.cdr.markForCheck();
      });
  }

  /** Bar height in % of the best sprint shown. */
  velocityHeight(points: number): number {
    const max = Math.max(...(this.facts?.velocity?.map(v => v.points) ?? [0]));
    return max > 0 ? Math.max(4, Math.round(points / max * 100)) : 4;
  }

  factText(kind: 'outlier' | 'confidence', t: SprintFactTicket): string {
    const name = t.externalKey ? `${t.externalKey} ${t.name}` : t.name;
    return kind === 'outlier'
      ? this.i18n.translate('retro.facts.outlierCard', { name, estimate: t.estimate ?? '', days: t.days, median: t.cardMedianDays })
      : this.i18n.translate('retro.facts.confidenceCard', { name, confidence: t.confidence });
  }

  pointsCard(): string {
    if (!this.facts) return '';
    return this.facts.plannedPoints != null
      ? this.i18n.translate('retro.facts.plannedCard', { planned: this.facts.plannedPoints, estimated: this.facts.estimatedPoints })
      : this.i18n.translate('retro.facts.estimatedCard', { points: this.facts.estimatedPoints });
  }
}
