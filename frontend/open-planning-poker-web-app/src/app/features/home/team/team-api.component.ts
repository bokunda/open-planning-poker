import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Apollo, gql } from 'apollo-angular';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { OPP_SNACKBAR_DURATION_DEFAULT } from '../../../shared/constants';

const ERRORS = `errors { ... on ApplicationError { code message } }`;

export const TEAM_API = gql`
  query TeamApi($id: UUID!) {
    team(id: $id) { ... on Team { id apiTokens { id name prefix createdOn lastUsedOn active } webhooks { id url events enabled createdOn } } }
  }`;
export const CREATE_TEAM_API_TOKEN = gql`
  mutation CreateTeamApiToken($input: CreateTeamApiTokenInput!) { createTeamApiToken(input: $input) { createdTeamApiToken { token } ${ERRORS} } }`;
export const REVOKE_TEAM_API_TOKEN = gql`
  mutation RevokeTeamApiToken($input: RevokeTeamApiTokenInput!) { revokeTeamApiToken(input: $input) { boolean ${ERRORS} } }`;
export const CREATE_TEAM_WEBHOOK = gql`
  mutation CreateTeamWebhook($input: CreateTeamWebhookInput!) { createTeamWebhook(input: $input) { createdTeamWebhook { secret } ${ERRORS} } }`;
export const UPDATE_TEAM_WEBHOOK = gql`
  mutation UpdateTeamWebhook($input: UpdateTeamWebhookInput!) { updateTeamWebhook(input: $input) { teamWebhookInfo { id } ${ERRORS} } }`;
export const ROTATE_TEAM_WEBHOOK_SECRET = gql`
  mutation RotateTeamWebhookSecret($input: RotateTeamWebhookSecretInput!) { rotateTeamWebhookSecret(input: $input) { createdTeamWebhook { secret } ${ERRORS} } }`;
export const DELETE_TEAM_WEBHOOK = gql`
  mutation DeleteTeamWebhook($input: DeleteTeamWebhookInput!) { deleteTeamWebhook(input: $input) { boolean ${ERRORS} } }`;
export const TEAM_WEBHOOK_DELIVERIES = gql`
  query TeamWebhookDeliveries($teamId: UUID!, $webhookId: UUID!) {
    teamWebhookDeliveries(teamId: $teamId, webhookId: $webhookId) {
      ... on TeamWebhookDeliveries { items { id event attempt statusCode success error durationMs createdOn } }
      ... on ApplicationError { code message }
    }
  }`;

export const WEBHOOK_EVENTS = ['ticket.estimated', 'session.finished'] as const;

interface ApiToken { id: string; name: string; prefix: string; createdOn: string; lastUsedOn: string | null; active: boolean }
interface Webhook { id: string; url: string; events: string[]; enabled: boolean; createdOn: string }
interface Delivery { id: string; event: string; attempt: number; statusCode: number | null; success: boolean; error: string | null; durationMs: number; createdOn: string }
type Payload = { errors?: { code: string; message: string }[] | null } | null | undefined;

/**
 * Public API of a team (OPP-158, OPP-160), for the team owner: API tokens for scripts and tools like Zapier or n8n,
 * and signed webhooks for "ticket estimated" and "session finished". Tokens and secrets are shown once.
 */
@Component({
  selector: 'app-team-api',
  standalone: true,
  imports: [TranslocoPipe, FormsModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="team-card" aria-labelledby="api-title">
      <h2 class="team-section-title" id="api-title">{{ 'api.title' | transloco }}</h2>
      <p class="muted">{{ 'api.lead' | transloco }} <a class="link" href="https://openplanningpoker.com/public-api/" target="_blank" rel="noopener">{{ 'api.docs' | transloco }}</a></p>

      @if (shown) {
        <div class="once" role="status">
          <p><strong>{{ shown.label | transloco }}</strong> {{ 'api.showOnce' | transloco }}</p>
          <code class="secret">{{ shown.value }}</code>
          <div class="row">
            <button mat-stroked-button type="button" (click)="copy(shown.value)">{{ 'api.copy' | transloco }}</button>
            <button mat-button type="button" (click)="shown = null">{{ 'api.done' | transloco }}</button>
          </div>
        </div>
      }

      <h3 class="sub">{{ 'api.tokens' | transloco }}</h3>
      @if (tokens.length) {
        <ul class="list">
          @for (t of tokens; track t.id) {
            <li [class.off]="!t.active">
              <span><strong>{{ t.name }}</strong> <code>{{ t.prefix }}…</code></span>
              <span class="muted">{{ t.active ? (t.lastUsedOn ? ('api.lastUsed' | transloco: { date: day(t.lastUsedOn) }) : ('api.neverUsed' | transloco)) : ('api.revoked' | transloco) }}</span>
              @if (t.active) { <button mat-button type="button" (click)="revoke(t)">{{ 'api.revoke' | transloco }}</button> }
            </li>
          }
        </ul>
      }
      <form class="row" (ngSubmit)="createToken()">
        <label class="sr" for="token-name">{{ 'api.tokenName' | transloco }}</label>
        <input id="token-name" name="tokenName" maxlength="60" [(ngModel)]="tokenName" [placeholder]="'api.tokenNamePlaceholder' | transloco">
        <button mat-flat-button color="primary" type="submit" [disabled]="!tokenName.trim() || busy">{{ 'api.createToken' | transloco }}</button>
      </form>

      <h3 class="sub">{{ 'api.webhooks' | transloco }}</h3>
      @if (webhooks.length) {
        <ul class="list">
          @for (w of webhooks; track w.id) {
            <li [class.off]="!w.enabled">
              <span class="url">{{ w.url }}</span>
              <span class="muted">{{ w.events.join(', ') }}</span>
              <span class="row">
                <button mat-button type="button" (click)="toggle(w)">{{ (w.enabled ? 'api.pause' : 'api.resume') | transloco }}</button>
                <button mat-button type="button" (click)="showDeliveries(w)">{{ 'api.deliveries' | transloco }}</button>
                <button mat-button type="button" (click)="rotate(w)">{{ 'api.rotate' | transloco }}</button>
                <button mat-button type="button" (click)="remove(w)">{{ 'api.delete' | transloco }}</button>
              </span>
              @if (deliveriesFor === w.id) {
                @if (!deliveries.length) {
                  <p class="muted">{{ 'api.noDeliveries' | transloco }}</p>
                } @else {
                  <div class="table-wrap">
                    <table>
                      <caption class="sr">{{ 'api.deliveries' | transloco }}</caption>
                      <thead><tr><th scope="col">{{ 'api.when' | transloco }}</th><th scope="col">{{ 'api.event' | transloco }}</th><th scope="col">{{ 'api.status' | transloco }}</th><th scope="col">{{ 'api.attempt' | transloco }}</th></tr></thead>
                      <tbody>
                        @for (d of deliveries; track d.id) {
                          <tr>
                            <td>{{ time(d.createdOn) }}</td>
                            <td>{{ d.event }}</td>
                            <td [class.bad]="!d.success">{{ d.success ? (d.statusCode ?? 'OK') : (d.error ?? d.statusCode) }}</td>
                            <td class="opp-tabular">{{ d.attempt }}</td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                }
              }
            </li>
          }
        </ul>
      }
      <form class="webhook-form" (ngSubmit)="createWebhook()">
        <label class="sr" for="webhook-url">{{ 'api.webhookUrl' | transloco }}</label>
        <input id="webhook-url" name="webhookUrl" type="url" maxlength="512" [(ngModel)]="webhookUrl" placeholder="https://hooks.zapier.com/…">
        <fieldset class="events">
          <legend class="sr">{{ 'api.events' | transloco }}</legend>
          @for (e of events; track e) {
            <label><input type="checkbox" [name]="'ev-' + e" [ngModel]="chosen.has(e)" (ngModelChange)="pick(e, $event)"> {{ e }}</label>
          }
        </fieldset>
        <button mat-flat-button color="primary" type="submit" [disabled]="!webhookUrl.trim() || !chosen.size || busy">{{ 'api.addWebhook' | transloco }}</button>
      </form>
      <p class="muted">{{ 'api.webhookHint' | transloco }}</p>
    </section>
  `,
  styles: [`
    :host { display: contents; }
    .team-card { display: grid; gap: var(--opp-space-3); align-content: start; padding: var(--opp-space-5); border: 1px solid var(--opp-border);
      border-radius: var(--opp-radius-lg); background: var(--opp-surface); min-width: 0; }
    .team-section-title { margin: 0; font-size: var(--opp-font-size-lg); }
    .sub { margin: var(--opp-space-2) 0 0; font-size: var(--opp-font-size-md, 1rem); }
    .muted { color: var(--opp-text-secondary); font-size: var(--opp-font-size-sm); margin: 0; }
    .link { color: var(--opp-primary); }
    .row { display: flex; flex-wrap: wrap; align-items: center; gap: var(--opp-space-2); }
    .list { list-style: none; margin: 0; padding: 0; display: grid; gap: var(--opp-space-2); }
    .list li { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: var(--opp-space-2);
      padding: var(--opp-space-2) 0; border-bottom: 1px solid var(--opp-border); }
    .list li.off { opacity: .6; }
    .url { overflow-wrap: anywhere; font-weight: 600; }
    input:not([type=checkbox]) { flex: 1; min-width: 0; padding: 8px 10px; border: 1px solid var(--opp-border); border-radius: var(--opp-radius-md);
      background: var(--opp-surface); color: var(--opp-text); font: inherit; }
    .webhook-form { display: grid; gap: var(--opp-space-2); }
    .events { display: flex; flex-wrap: wrap; gap: var(--opp-space-4); border: 0; margin: 0; padding: 0; font-size: var(--opp-font-size-sm); }
    .once { display: grid; gap: var(--opp-space-2); padding: var(--opp-space-3); border: 1px solid var(--opp-primary); border-radius: var(--opp-radius-md); }
    .once p { margin: 0; }
    .secret { display: block; padding: var(--opp-space-2); background: var(--opp-bg); border-radius: var(--opp-radius-sm); overflow-wrap: anywhere; user-select: all; }
    .table-wrap { overflow-x: auto; width: 100%; }
    table { width: 100%; border-collapse: collapse; font-size: var(--opp-font-size-sm); }
    th, td { text-align: left; padding: 4px 8px; border-bottom: 1px solid var(--opp-border); white-space: nowrap; }
    .bad { color: var(--opp-error, #c62828); white-space: normal; }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
  `],
})
export class TeamApiComponent implements OnChanges {
  @Input() teamId = '';

  private readonly apollo = inject(Apollo);
  private readonly snackBar = inject(MatSnackBar);
  private readonly i18n = inject(TranslocoService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly events = WEBHOOK_EVENTS;
  tokens: ApiToken[] = [];
  webhooks: Webhook[] = [];
  deliveries: Delivery[] = [];
  deliveriesFor: string | null = null;
  shown: { label: string; value: string } | null = null;
  tokenName = '';
  webhookUrl = '';
  chosen = new Set<string>(WEBHOOK_EVENTS);
  busy = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['teamId'] && this.teamId) this.load();
  }

  private load(): void {
    this.apollo.query<{ team: { apiTokens?: ApiToken[]; webhooks?: Webhook[] } }>({ query: TEAM_API, variables: { id: this.teamId }, fetchPolicy: 'no-cache' })
      .subscribe(({ data }) => {
        this.tokens = data?.team?.apiTokens ?? [];
        this.webhooks = data?.team?.webhooks ?? [];
        this.cdr.markForCheck();
      });
  }

  day(value: string): string { return new Date(value).toLocaleDateString(this.i18n.getActiveLang()); }
  time(value: string): string { return new Date(value).toLocaleString(this.i18n.getActiveLang()); }

  pick(event: string, on: boolean): void {
    if (on) this.chosen.add(event); else this.chosen.delete(event);
  }

  createToken(): void {
    const name = this.tokenName.trim();
    if (!name) return;
    this.run<{ createdTeamApiToken?: { token: string } }>(CREATE_TEAM_API_TOKEN, 'createTeamApiToken', { teamId: this.teamId, name }, result => {
      this.tokenName = '';
      if (result?.createdTeamApiToken) this.shown = { label: 'api.newToken', value: result.createdTeamApiToken.token };
    });
  }

  revoke(token: ApiToken): void {
    this.run(REVOKE_TEAM_API_TOKEN, 'revokeTeamApiToken', { teamId: this.teamId, tokenId: token.id });
  }

  createWebhook(): void {
    const url = this.webhookUrl.trim();
    if (!url || !this.chosen.size) return;
    this.run<{ createdTeamWebhook?: { secret: string } }>(CREATE_TEAM_WEBHOOK, 'createTeamWebhook', { teamId: this.teamId, url, events: [...this.chosen] }, result => {
      this.webhookUrl = '';
      if (result?.createdTeamWebhook) this.shown = { label: 'api.newSecret', value: result.createdTeamWebhook.secret };
    });
  }

  toggle(w: Webhook): void {
    this.run(UPDATE_TEAM_WEBHOOK, 'updateTeamWebhook', { teamId: this.teamId, webhookId: w.id, url: w.url, events: w.events, enabled: !w.enabled });
  }

  rotate(w: Webhook): void {
    this.run<{ createdTeamWebhook?: { secret: string } }>(ROTATE_TEAM_WEBHOOK_SECRET, 'rotateTeamWebhookSecret', { teamId: this.teamId, webhookId: w.id }, result => {
      if (result?.createdTeamWebhook) this.shown = { label: 'api.newSecret', value: result.createdTeamWebhook.secret };
    });
  }

  remove(w: Webhook): void {
    this.run(DELETE_TEAM_WEBHOOK, 'deleteTeamWebhook', { teamId: this.teamId, webhookId: w.id });
  }

  showDeliveries(w: Webhook): void {
    if (this.deliveriesFor === w.id) { this.deliveriesFor = null; return; }
    this.deliveriesFor = w.id;
    this.deliveries = [];
    this.apollo.query<{ teamWebhookDeliveries: { items?: Delivery[] } }>({
      query: TEAM_WEBHOOK_DELIVERIES, variables: { teamId: this.teamId, webhookId: w.id }, fetchPolicy: 'no-cache',
    }).subscribe(({ data }) => { this.deliveries = data?.teamWebhookDeliveries?.items ?? []; this.cdr.markForCheck(); });
  }

  copy(value: string): void {
    navigator.clipboard?.writeText(value).then(() => this.notify(this.i18n.translate('api.copied')));
  }

  private run<T extends object>(mutation: ReturnType<typeof gql>, field: string, input: Record<string, unknown>, done?: (result: T | undefined) => void): void {
    this.busy = true;
    this.apollo.mutate<Record<string, Payload & T>>({ mutation, variables: { input } }).subscribe({
      next: ({ data }) => {
        this.busy = false;
        const result = data?.[field];
        if (result?.errors?.length) { this.notify(result.errors[0].message); this.cdr.markForCheck(); return; }
        done?.(result as T | undefined);
        this.load();
      },
      error: () => { this.busy = false; this.notify(this.i18n.translate('common.tryAgainNetwork')); this.cdr.markForCheck(); },
    });
  }

  private notify(message: string): void {
    this.snackBar.open(message, undefined, { duration: OPP_SNACKBAR_DURATION_DEFAULT });
  }
}
