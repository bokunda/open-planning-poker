import { Component, OnInit, PLATFORM_ID, inject, ChangeDetectionStrategy } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Apollo } from 'apollo-angular';
import { AppError, COMPLETE_JIRA_CONNECTION, JiraSite, SELECT_JIRA_SITE } from '../home/game/gql/jira.graphql';

/**
 * Atlassian sends the browser here after consent (OPP-10): the code is exchanged on the server, the host
 * picks a site when there are several, and goes back to the game they came from.
 */
@Component({
  selector: 'app-jira-callback',
  standalone: true,
  imports: [MatButtonModule, MatProgressSpinnerModule, TranslocoPipe],
  template: `
    <main id="main" class="jira-callback">
      <section class="card">
        <h1>{{ 'jira.callbackTitle' | transloco }}</h1>
        @if (error) {
          <p role="alert">{{ error }}</p>
          <button mat-flat-button color="primary" (click)="back()">{{ 'jira.back' | transloco }}</button>
        } @else if (sites.length > 1) {
          <p>{{ 'jira.chooseSite' | transloco }}</p>
          <div class="sites">
            @for (site of sites; track site.cloudId) {
              <button mat-stroked-button (click)="select(site)" [disabled]="busy">{{ site.name }}<small>{{ site.url }}</small></button>
            }
          </div>
        } @else {
          <mat-spinner diameter="32"></mat-spinner>
          <p>{{ 'jira.connecting' | transloco }}</p>
        }
      </section>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .jira-callback { display: grid; place-items: center; min-height: 70vh; padding: var(--opp-space-4); }
    .card { display: grid; gap: var(--opp-space-4); justify-items: center; max-width: 460px; width: 100%; padding: var(--opp-space-6);
      background: var(--opp-surface); border: 1px solid var(--opp-border); border-radius: var(--opp-radius-xl); text-align: center; }
    h1 { margin: 0; font-family: var(--opp-font-display); font-size: 1.5rem; }
    .sites { display: grid; gap: var(--opp-space-2); width: 100%; }
    .sites button { display: grid; height: auto; padding: var(--opp-space-2) var(--opp-space-3); }
    .sites small { color: var(--opp-text-muted); }
  `]
})
export class JiraCallbackComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly apollo = inject(Apollo);
  private readonly i18n = inject(TranslocoService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  sites: JiraSite[] = [];
  error = '';
  busy = false;
  private returnPath = '/game';

  ngOnInit(): void {
    // The token is in the browser's storage; nothing to do while rendering on the server.
    if (!this.isBrowser) return;
    const params = this.route.snapshot.queryParamMap;
    const code = params.get('code');
    const state = params.get('state');
    if (!code || !state) {
      this.error = params.get('error_description') ?? params.get('error') ?? this.i18n.translate('jira.failed');
      return;
    }
    this.apollo.mutate<{ completeJiraConnection: {
      jiraConnectionResult?: { returnPath: string; siteSelected: boolean; sites: JiraSite[] }; errors?: AppError[] } }>({
      mutation: COMPLETE_JIRA_CONNECTION, variables: { input: { code, state } }
    }).subscribe({
      next: ({ data }) => {
        const result = data?.completeJiraConnection;
        if (result?.errors?.length || !result?.jiraConnectionResult) {
          this.error = result?.errors?.[0]?.message ?? this.i18n.translate('jira.failed');
          return;
        }
        this.returnPath = result.jiraConnectionResult.returnPath || '/game';
        if (result.jiraConnectionResult.siteSelected || result.jiraConnectionResult.sites.length === 0) {
          this.back();
          return;
        }
        this.sites = result.jiraConnectionResult.sites;
      },
      error: () => { this.error = this.i18n.translate('jira.failed'); }
    });
  }

  select(site: JiraSite): void {
    this.busy = true;
    this.apollo.mutate<{ selectJiraSite: { errors?: AppError[] } }>({
      mutation: SELECT_JIRA_SITE, variables: { input: { cloudId: site.cloudId } }
    }).subscribe(({ data }) => {
      this.busy = false;
      if (data?.selectJiraSite?.errors?.length) {
        this.error = data.selectJiraSite.errors[0].message;
        return;
      }
      this.back();
    });
  }

  back(): void {
    this.router.navigateByUrl(this.returnPath, { replaceUrl: true });
  }
}
