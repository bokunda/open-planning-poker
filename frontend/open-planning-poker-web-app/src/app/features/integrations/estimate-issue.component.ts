import { Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Apollo, gql } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import { Mutation, Query } from '../../graphql/graphql-gateway.service';
import { CREATE_GAME } from '../home/game/gql/createGame.graphql';
import { GET_USER } from '../home/user/gql/getUser.graphql';
import { REGISTER_USER } from '../home/user/gql/registerUser.graphql';

const CREATE_TICKET_FOR_ISSUE = gql`
  mutation CreateTicketForIssue($input: CreateTicketForIssueInput!) {
    createTicketForIssue(input: $input) {
      ticket { id }
      errors { ... on ApplicationError { code message } }
    }
  }`;

const JIRA_KEY = /^[A-Z][A-Z0-9_]{0,30}-\d{1,12}$/;

/** Accepts only https links to a Jira Cloud site, like the server does. */
export function isJiraIssueUrl(url: string): boolean {
  try {
    const u = new URL(url);
    return u.protocol === 'https:' && u.hostname.endsWith('.atlassian.net');
  } catch {
    return false;
  }
}

/**
 * Starts a game for one Jira issue (OPP-120): /estimate?key=OPP-118&title=…&url=https://acme.atlassian.net/browse/OPP-118
 * The Jira issue panel opens this page. The current (anonymous) user becomes the host, the issue becomes the first
 * ticket, linked so "Save to KEY" works after the reveal, and the page moves on to the game.
 */
@Component({
  selector: 'app-estimate-issue',
  standalone: true,
  imports: [TranslocoPipe, RouterLink, MatButtonModule],
  template: `
    <main class="estimate">
      @if (error) {
        <h1>{{ 'estimateIssue.failedTitle' | transloco }}</h1>
        <p>{{ error }}</p>
        <a mat-flat-button color="primary" routerLink="/game">{{ 'estimateIssue.openApp' | transloco }}</a>
      } @else {
        <h1>{{ 'estimateIssue.starting' | transloco: { key: key } }}</h1>
        <p>{{ 'estimateIssue.lead' | transloco }}</p>
      }
    </main>
  `,
  styles: [`
    .estimate { max-width: 560px; margin: 0 auto; padding: var(--opp-space-8, 48px) var(--opp-space-4, 16px); text-align: center; }
    h1 { font-family: var(--opp-font-display); font-size: var(--opp-font-size-2xl, 1.5rem); color: var(--opp-text); }
    p { color: var(--opp-text-secondary); }
  `],
})
export class EstimateIssueComponent implements OnInit {
  key = '';
  error = '';

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly apollo = inject(Apollo);
  private readonly i18n = inject(TranslocoService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    this.key = (params.get('key') ?? '').trim().toUpperCase();
    const title = (params.get('title') ?? '').trim().slice(0, 200);
    const url = (params.get('url') ?? '').trim();
    if (!JIRA_KEY.test(this.key) || !isJiraIssueUrl(url)) {
      this.error = this.i18n.translate('estimateIssue.invalid');
      return;
    }
    if (!this.isBrowser) return;
    this.start(title, url).catch(() => this.error = this.i18n.translate('estimateIssue.failed'));
  }

  private async start(title: string, url: string): Promise<void> {
    await this.ensureUser();
    const created = await firstValueFrom(this.apollo.mutate<Mutation>({
      mutation: CREATE_GAME,
      variables: { input: { name: `${this.key} ${title}`.trim().slice(0, 100), description: '' } },
    }));
    const gameId = (created.data?.createGame?.game as { id?: string } | undefined)?.id;
    if (!gameId) throw new Error('createGame failed');
    const ticket = await firstValueFrom(this.apollo.mutate<{ createTicketForIssue: { ticket?: { id: string }; errors?: { message: string }[] } }>({
      mutation: CREATE_TICKET_FOR_ISSUE,
      variables: { input: { gameId, key: this.key, title, url } },
    }));
    const payload = ticket.data?.createTicketForIssue;
    if (payload?.errors?.length) {
      this.error = payload.errors[0].message;
      return;
    }
    await this.router.navigate(payload?.ticket?.id ? ['/game', gameId, 'ticket', payload.ticket.id] : ['/game', gameId], { replaceUrl: true });
  }

  private async ensureUser(): Promise<void> {
    if (localStorage.getItem('token')) {
      const current = await firstValueFrom(this.apollo.query<Query>({ query: GET_USER })).catch(() => null);
      if ((current?.data?.currentUser as { id?: string } | undefined)?.id) return;
    }
    const registered = await firstValueFrom(this.apollo.mutate<Mutation>({ mutation: REGISTER_USER, variables: {} }));
    const token = registered.data?.registerUser?.registerUserResponse?.token;
    if (!token) throw new Error('registerUser failed');
    localStorage.setItem('token', token);
  }
}
