import { Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Apollo } from 'apollo-angular';
import { COMPLETE_TRACKER_CONNECTION } from '../home/game/gql/trackers.graphql';

/**
 * GitHub, Linear and Azure DevOps send the browser here after consent (OPP-116):
 * /integrations/tracker/:provider/callback. The code is exchanged on the server, then the host goes back to the game.
 */
@Component({
  selector: 'app-tracker-callback',
  standalone: true,
  imports: [MatButtonModule, MatProgressSpinnerModule, TranslocoPipe],
  template: `
    <main id="main" class="tracker-callback">
      <section class="card">
        <h1>{{ 'trackers.callbackTitle' | transloco }}</h1>
        @if (error) {
          <p role="alert">{{ error }}</p>
          <button mat-flat-button color="primary" (click)="back()">{{ 'jira.back' | transloco }}</button>
        } @else {
          <mat-spinner diameter="32"></mat-spinner>
        }
      </section>
    </main>
  `,
  styles: [`
    .tracker-callback { display: grid; place-items: center; min-height: 70vh; padding: var(--opp-space-4); }
    .card { display: grid; gap: var(--opp-space-4); justify-items: center; max-width: 460px; width: 100%; padding: var(--opp-space-6);
      background: var(--opp-surface); border: 1px solid var(--opp-border); border-radius: var(--opp-radius-xl); text-align: center; }
    h1 { margin: 0; font-family: var(--opp-font-display); font-size: 1.5rem; }
  `]
})
export class TrackerCallbackComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly apollo = inject(Apollo);
  private readonly i18n = inject(TranslocoService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  error = '';
  private returnPath = '/game';

  ngOnInit(): void {
    if (!this.isBrowser) return;
    const provider = this.route.snapshot.paramMap.get('provider') ?? '';
    const params = this.route.snapshot.queryParamMap;
    const code = params.get('code');
    const state = params.get('state');
    if (!code || !state) {
      this.error = params.get('error_description') ?? params.get('error') ?? this.i18n.translate('trackers.failed');
      return;
    }
    this.apollo.mutate<{ completeTrackerConnection: { trackerConnected?: { returnPath: string }; errors?: { message: string }[] } }>({
      mutation: COMPLETE_TRACKER_CONNECTION, variables: { input: { provider, code, state } }
    }).subscribe({
      next: ({ data }) => {
        const result = data?.completeTrackerConnection;
        if (result?.errors?.length || !result?.trackerConnected) {
          this.error = result?.errors?.[0]?.message ?? this.i18n.translate('trackers.failed');
          return;
        }
        this.returnPath = result.trackerConnected.returnPath || '/game';
        this.back();
      },
      error: () => this.error = this.i18n.translate('trackers.failed')
    });
  }

  back(): void {
    this.router.navigateByUrl(this.returnPath);
  }
}
