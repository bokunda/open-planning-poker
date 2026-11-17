import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, PLATFORM_ID, SimpleChanges, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Apollo } from 'apollo-angular';
import { Subscription } from 'rxjs';
import { TranslocoPipe } from '@jsverse/transloco';
import { TICKET_REFERENCE, TeamAnchor, TeamTicket } from '../../team/team.graphql';
import { byCard } from '../../team/team-reference.component';

interface Reference { teamId: string | null; anchors: TeamAnchor[]; similar: TeamTicket[] }

/**
 * "Reference" next to the cards in a team session (OPP-128): the team's anchor tickets by card value and up to three
 * similar tickets from past sprints, so the team compares before it votes. Hidden outside team rooms.
 */
@Component({
  selector: 'app-reference-panel',
  standalone: true,
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (ref && (ref.anchors.length || ref.similar.length)) {
      <details class="reference" [open]="open" (toggle)="open = $any($event.target).open">
        <summary>{{ 'reference.panelTitle' | transloco }}</summary>
        @if (ref.anchors.length) {
          <p class="caption">{{ 'reference.anchorsCaption' | transloco }}</p>
          <ul>
            @for (a of anchors; track a.id) {
              <li><b class="card opp-tabular">{{ a.value }}</b> <span>{{ a.ticket.name }}@if (a.note) { <span class="muted"> · {{ a.note }}</span> }</span></li>
            }
          </ul>
        }
        @if (ref.similar.length) {
          <p class="caption">{{ 'reference.similarCaption' | transloco }}</p>
          <ul>
            @for (t of ref.similar; track t.ticketId) {
              <li><b class="card opp-tabular">{{ t.estimate }}</b> <span>{{ t.name }} <span class="muted">· {{ t.sprintName }}</span></span></li>
            }
          </ul>
        }
      </details>
    }
  `,
  styles: [`
    :host { display: block; width: 100%; max-width: 560px; margin: var(--opp-space-2) auto; }
    .reference { border: 1px solid var(--opp-border); border-radius: var(--opp-radius-lg); background: var(--opp-surface); padding: var(--opp-space-2) var(--opp-space-4); }
    summary { cursor: pointer; font-weight: 700; color: var(--opp-text); padding: var(--opp-space-1) 0; }
    summary:focus-visible { outline: 3px solid var(--opp-focus-ring, var(--opp-primary)); outline-offset: 2px; border-radius: 4px; }
    .caption { margin: var(--opp-space-2) 0 var(--opp-space-1); font-size: var(--opp-font-size-xs); color: var(--opp-text-muted); text-transform: uppercase; letter-spacing: .04em; }
    ul { list-style: none; margin: 0; padding: 0; display: grid; gap: var(--opp-space-1); }
    li { display: flex; gap: var(--opp-space-2); align-items: baseline; color: var(--opp-text); overflow-wrap: anywhere; font-size: var(--opp-font-size-sm); }
    .card { min-width: 28px; text-align: center; padding: 0 4px; border-radius: var(--opp-radius-sm, 6px); background: var(--opp-primary-50); color: var(--opp-primary); font-family: var(--opp-font-display); }
    .muted { color: var(--opp-text-secondary); }
  `],
})
export class ReferencePanelComponent implements OnChanges {
  @Input() ticketId = '';
  /** Only team sessions have a reference. */
  @Input() teamId: string | null = null;

  ref: Reference | null = null;
  anchors: TeamAnchor[] = [];
  open = false;

  private readonly apollo = inject(Apollo);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private load: Subscription | undefined;

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['ticketId'] && !changes['teamId']) return;
    this.load?.unsubscribe();
    this.ref = null;
    if (!this.teamId || !this.ticketId || !this.isBrowser) return;
    this.load = this.apollo.query<{ ticketReference: Reference & { code?: string } }>({ query: TICKET_REFERENCE, variables: { ticketId: this.ticketId } })
      .subscribe(({ data }) => {
        const ref = data?.ticketReference;
        this.ref = ref && !ref.code ? ref : null;
        this.anchors = [...(this.ref?.anchors ?? [])].sort((a, b) => byCard(a.value, b.value));
        this.cdr.markForCheck();
      });
  }
}
