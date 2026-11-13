import { ChangeDetectionStrategy, Component, Input, OnChanges, inject } from '@angular/core';

import { Sprint } from './team.graphql';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OppDatePipe } from '../../../i18n/opp-date.pipe';

interface Bar {
  sprint: Sprint;
  x: number;
  y: number;
  height: number;
}

/**
 * Points estimated per sprint, oldest left. One series, so no legend: the heading names it.
 * Each bar has a hover/focus tooltip, a dashed line marks the average of the last three
 * sprints, and the same numbers are in a table below (the accessible view).
 */
@Component({
  selector: 'app-velocity-chart',
  standalone: true,
  imports: [TranslocoPipe, OppDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (bars.length) {
      <figure class="velocity">
        <div class="chart-wrap">
          <svg class="chart" [attr.viewBox]="'0 0 ' + width + ' ' + height" preserveAspectRatio="none"
            role="img" [attr.aria-label]="ariaLabel">
            <!-- recessive grid -->
            <g class="grid">
              @for (t of ticks; track t) {
                <line x1="0" [attr.x2]="width" [attr.y1]="yFor(t)" [attr.y2]="yFor(t)"></line>
              }
            </g>
            <line class="baseline" x1="0" [attr.x2]="width" [attr.y1]="plotBottom" [attr.y2]="plotBottom"></line>
            @if (average !== null) {
              <line class="average" x1="0" [attr.x2]="width" [attr.y1]="yFor(average)" [attr.y2]="yFor(average)"></line>
            }
            <!-- bars: 4px rounded top, square at the baseline -->
            @for (bar of bars; track bar) {
              <path class="bar" [attr.opacity]="active && bar !== active ? 0.45 : 1" [attr.d]="barPath(bar)"></path>
            }
            <!-- hit targets wider than the bars -->
            @for (bar of bars; track bar) {
              <rect class="hit" [attr.x]="bar.x - gap / 2" y="0" [attr.width]="barWidth + gap" [attr.height]="plotBottom"
                tabindex="0" [attr.aria-label]="label(bar.sprint)"
              (mouseenter)="active = bar" (mouseleave)="active = null" (focus)="active = bar" (blur)="active = null"></rect>
            }
          </svg>
          <div class="axis-y" aria-hidden="true">
            @for (t of ticks; track t) {
              <span [style.top.%]="(yFor(t) / height) * 100">{{ t }}</span>
            }
          </div>
          @if (active) {
            <div class="tooltip" [style.left.%]="((active.x + barWidth / 2) / width) * 100"
              [style.top.%]="(active.y / height) * 100" role="status">
              <b class="opp-tabular">{{ active.sprint.points }} {{ 'team.pts' | transloco }}</b>
              <span>{{ active.sprint.name }}</span>
              <span class="tooltip-muted">{{ 'team.ticketsOf' | transloco: { count: active.sprint.estimatedCount, total: active.sprint.ticketCount } }} · {{ active.sprint.createdOn | oppDate:'d MMM' }}</span>
            </div>
          }
        </div>
        <div class="axis-x" aria-hidden="true">
          @for (bar of bars; track bar) {
            <span [style.left.%]="((bar.x + barWidth / 2) / width) * 100">{{ bar.sprint.createdOn | oppDate:'d MMM' }}</span>
          }
        </div>
        @if (average !== null) {
          <figcaption class="caption">
            <span class="caption-line" aria-hidden="true"></span> {{ 'team.averageCaption' | transloco }} <b class="opp-tabular">{{ average }}</b> {{ 'team.points' | transloco }}
          </figcaption>
        }
        <details class="table-view">
          <summary>{{ 'team.showTable' | transloco }}</summary>
          <table>
            <thead><tr><th scope="col">{{ 'team.sprint' | transloco }}</th><th scope="col">{{ 'team.date' | transloco }}</th><th scope="col" class="num">{{ 'team.pointsTitle' | transloco }}</th><th scope="col" class="num">{{ 'team.estimatedTickets' | transloco }}</th></tr></thead>
            <tbody>
              @for (bar of bars; track bar) {
                <tr>
                  <th scope="row">{{ bar.sprint.name }}</th>
                  <td>{{ bar.sprint.createdOn | oppDate:'d MMM y' }}</td>
                  <td class="num opp-tabular">{{ bar.sprint.points }}</td>
                  <td class="num opp-tabular">{{ bar.sprint.estimatedCount }} / {{ bar.sprint.ticketCount }}</td>
                </tr>
              }
            </tbody>
          </table>
        </details>
      </figure>
    } @else {
      <p class="velocity-empty">{{ 'team.chartEmpty' | transloco }}</p>
    }
    `,
  styles: [`
    :host { display: block; }
    .velocity { margin: 0; display: grid; gap: var(--opp-space-2); }
    .chart-wrap { position: relative; height: 200px; margin-left: 32px; }
    .chart { width: 100%; height: 100%; overflow: visible; display: block; }
    .grid line { stroke: var(--opp-border-light, var(--opp-border)); stroke-width: 1; vector-effect: non-scaling-stroke; }
    .baseline { stroke: var(--opp-border); stroke-width: 1; vector-effect: non-scaling-stroke; }
    .average { stroke: var(--opp-text-secondary); stroke-width: 1.5; stroke-dasharray: 4 4; vector-effect: non-scaling-stroke; }
    .bar { fill: var(--opp-primary); transition: opacity var(--opp-transition-fast); }
    .hit { fill: transparent; cursor: default; outline: none; }
    .hit:focus-visible { stroke: var(--opp-focus-ring, var(--opp-primary)); stroke-width: 2; vector-effect: non-scaling-stroke; }
    .axis-y { position: absolute; inset: 0 auto 0 -32px; width: 28px; }
    .axis-y span { position: absolute; right: 0; transform: translateY(-50%); font-size: 11px; color: var(--opp-text-muted); }
    .axis-x { position: relative; height: 16px; margin-left: 32px; }
    .axis-x span { position: absolute; transform: translateX(-50%); font-size: 11px; color: var(--opp-text-muted); white-space: nowrap; }
    .tooltip {
      position: absolute; transform: translate(-50%, calc(-100% - 8px)); pointer-events: none; z-index: 1;
      display: grid; gap: 2px; min-width: 140px; padding: 8px 10px;
      background: var(--opp-surface); border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md);
      box-shadow: var(--opp-shadow-md, 0 4px 16px rgba(0,0,0,.12)); font-size: var(--opp-font-size-xs); color: var(--opp-text);
    }
    .tooltip b { font-size: var(--opp-font-size-sm); }
    .tooltip-muted, .caption { color: var(--opp-text-secondary); }
    .caption { display: flex; align-items: center; gap: 6px; font-size: var(--opp-font-size-xs); }
    .caption b { color: var(--opp-text); }
    .caption-line { width: 18px; border-top: 1.5px dashed var(--opp-text-secondary); }
    .table-view summary { cursor: pointer; font-size: var(--opp-font-size-xs); color: var(--opp-text-secondary); }
    .table-view table { width: 100%; margin-top: var(--opp-space-2); border-collapse: collapse; font-size: var(--opp-font-size-sm); }
    .table-view th, .table-view td { padding: 6px 8px; border-bottom: 1px solid var(--opp-border-light, var(--opp-border)); text-align: left; font-weight: 400; }
    .table-view thead th { font-weight: 600; color: var(--opp-text-secondary); }
    .table-view .num { text-align: right; }
    .velocity-empty { margin: 0; color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); }
  `]
})
export class VelocityChartComponent implements OnChanges {
  /** Sprints newest first, as the API returns them. */
  @Input() sprints: Sprint[] = [];
  @Input() average: number | null = null;

  readonly width = 600;
  readonly height = 200;
  readonly plotBottom = 200;
  readonly gap = 12;
  barWidth = 0;
  bars: Bar[] = [];
  ticks: number[] = [];
  active: Bar | null = null;
  ariaLabel = '';
  private readonly i18n = inject(TranslocoService);
  private max = 1;

  ngOnChanges(): void {
    // Last ten sprints that estimated something, oldest first.
    const shown = this.sprints.filter(s => s.estimatedCount > 0).slice(0, 10).reverse();
    this.max = niceMax(Math.max(1, ...shown.map(s => s.points), this.average ?? 0));
    this.ticks = [0, this.max / 2, this.max].map(v => Math.round(v * 10) / 10);
    const slot = this.width / Math.max(shown.length, 4);
    this.barWidth = Math.min(48, slot - this.gap);
    const offset = (this.width - slot * shown.length) / 2;
    this.bars = shown.map((sprint, i) => {
      const y = this.yFor(sprint.points);
      return { sprint, x: offset + i * slot + (slot - this.barWidth) / 2, y, height: this.plotBottom - y };
    });
    this.ariaLabel = this.i18n.translate('team.chartAria', { list: shown.map(s => `${s.name} ${s.points}`).join(', ') });
  }

  yFor(value: number): number {
    return this.plotBottom - (value / this.max) * (this.plotBottom - 8);
  }

  barPath(bar: Bar): string {
    const r = Math.min(4, bar.height, this.barWidth / 2);
    const { x, y } = bar;
    const w = this.barWidth;
    const b = this.plotBottom;
    if (bar.height <= 0) return '';
    return `M${x},${b} V${y + r} Q${x},${y} ${x + r},${y} H${x + w - r} Q${x + w},${y} ${x + w},${y + r} V${b} Z`;
  }

  label(sprint: Sprint): string {
    return this.i18n.translate('team.barLabel', { name: sprint.name, points: sprint.points, count: sprint.estimatedCount, total: sprint.ticketCount });
  }
}

/** Rounds the axis maximum up to 1, 2, 2.5 or 5 times a power of ten. */
function niceMax(value: number): number {
  const power = Math.pow(10, Math.floor(Math.log10(value)));
  for (const step of [1, 2, 2.5, 5, 10]) {
    if (value <= step * power) return step * power;
  }
  return 10 * power;
}
