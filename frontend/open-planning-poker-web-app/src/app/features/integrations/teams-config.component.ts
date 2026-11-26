import { Component, OnInit, PLATFORM_ID, inject, ChangeDetectionStrategy } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Apollo } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import { Mutation, Query } from '../../graphql/graphql-gateway.service';
import { CREATE_GAME } from '../home/game/gql/createGame.graphql';
import { UPDATE_SETTINGS } from '../home/game/gql/updateSettings.graphql';
import { GET_USER } from '../home/user/gql/getUser.graphql';
import { REGISTER_USER } from '../home/user/gql/registerUser.graphql';
import { extractGameId } from '../home/game/join/extract-game-id';
import { DECK_PRESETS, DEFAULT_DECK } from '../../shared/deck-presets';
import { EMBED_TEAMS } from '../../shared/embed';

/** The part of the Teams JavaScript client this page uses. */
interface TeamsSdk {
  app: { initialize(): Promise<void> };
  pages: {
    config: {
      setValidityState(valid: boolean): void;
      registerOnSaveHandler(handler: (save: { notifySuccess(): void; notifyFailure(reason?: string): void }) => void): void;
      setConfig(config: { entityId: string; contentUrl: string; websiteUrl: string; suggestedDisplayName: string }): Promise<void>;
    };
  };
}

const TEAMS_SDK_URL = 'https://res.cdn.office.net/teams-js/2.22.0/js/MicrosoftTeams.min.js';
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Configuration page of the Microsoft Teams tab (OPP-23). Teams opens it when someone adds the app to a meeting,
 * a channel or a chat: it creates a game (the person adding the tab becomes the host) or takes the link of an
 * existing one, and the tab then shows that game to everyone.
 */
@Component({
  selector: 'app-teams-config',
  standalone: true,
  imports: [FormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonToggleModule, TranslocoPipe],
  template: `
    <main id="main-content" class="teams-config">
      <h1>{{ 'teamsTab.title' | transloco }}</h1>
      @if (notInTeams) {
        <p role="alert">{{ 'teamsTab.notInTeams' | transloco }}</p>
      } @else {
        <p>{{ 'teamsTab.lead' | transloco }}</p>
        <mat-button-toggle-group [(ngModel)]="mode" (change)="validate()" [attr.aria-label]="'teamsTab.title' | transloco">
          <mat-button-toggle value="new">{{ 'teamsTab.newGame' | transloco }}</mat-button-toggle>
          <mat-button-toggle value="existing">{{ 'teamsTab.existingGame' | transloco }}</mat-button-toggle>
        </mat-button-toggle-group>

        @if (mode === 'new') {
          <mat-form-field appearance="outline">
            <mat-label>{{ 'teamsTab.name' | transloco }}</mat-label>
            <input matInput [(ngModel)]="name" (ngModelChange)="validate()" maxlength="255" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'createGame.deck' | transloco }}</mat-label>
            <mat-select [(ngModel)]="deck">
              @for (preset of decks; track preset.value) {
                <mat-option [value]="preset.value">{{ preset.key | transloco }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <p class="note">{{ 'teamsTab.hostNote' | transloco }}</p>
        } @else {
          <mat-form-field appearance="outline">
            <mat-label>{{ 'teamsTab.link' | transloco }}</mat-label>
            <input matInput [(ngModel)]="link" (ngModelChange)="validate()" placeholder="https://app.openplanningpoker.com/game/…" />
            <mat-hint>{{ 'teamsTab.linkHint' | transloco }}</mat-hint>
            @if (link.trim() && !linkGameId) {
              <mat-error>{{ 'teamsTab.linkInvalid' | transloco }}</mat-error>
            }
          </mat-form-field>
        }
        @if (error) {
          <p role="alert" class="error">{{ error }}</p>
        }
      }
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .teams-config { display: grid; gap: var(--opp-space-3); max-width: 520px; margin: 0 auto; padding: var(--opp-space-5) var(--opp-space-4); }
    h1 { margin: 0; font-family: var(--opp-font-display); font-size: 1.375rem; }
    p { margin: 0; }
    mat-form-field { width: 100%; }
    .note { color: var(--opp-text-muted); font-size: .875rem; }
    .error { color: var(--opp-danger, #d13438); }
  `]
})
export class TeamsConfigComponent implements OnInit {
  private readonly apollo = inject(Apollo);
  private readonly i18n = inject(TranslocoService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private teams?: TeamsSdk;

  readonly decks = DECK_PRESETS;
  mode: 'new' | 'existing' = 'new';
  name = '';
  deck = DEFAULT_DECK;
  link = '';
  error = '';
  notInTeams = false;

  get linkGameId(): string | null {
    const id = extractGameId(this.link);
    return UUID.test(id) ? id : null;
  }

  ngOnInit(): void {
    if (!this.isBrowser) return;
    this.name = this.i18n.translate('teamsTab.defaultName');
    // Teams always opens this page in a frame.
    if (window.parent === window) {
      this.notInTeams = true;
      return;
    }
    this.start().catch(() => this.notInTeams = true);
  }

  private async start(): Promise<void> {
    const teams = await loadTeamsSdk();
    // Outside Teams the SDK never answers.
    await Promise.race([teams.app.initialize(), new Promise((_, reject) => setTimeout(() => reject(new Error('not in Teams')), 5000))]);
    this.teams = teams;
    teams.pages.config.registerOnSaveHandler(save => {
      this.save().then(() => save.notifySuccess(), (err: Error) => {
        this.error = this.i18n.translate('teamsTab.saveFailed');
        save.notifyFailure(err.message);
      });
    });
    this.validate();
  }

  validate(): void {
    this.error = '';
    const valid = this.mode === 'new' ? this.name.trim().length >= 3 : this.linkGameId !== null;
    this.teams?.pages.config.setValidityState(valid);
  }

  private async save(): Promise<void> {
    const gameId = this.mode === 'new' ? await this.createGame() : this.linkGameId!;
    const origin = window.location.origin;
    await this.teams!.pages.config.setConfig({
      entityId: `opp-${gameId}`,
      contentUrl: `${origin}/game/${gameId}?embed=${EMBED_TEAMS}`,
      websiteUrl: `${origin}/game/${gameId}`,
      suggestedDisplayName: this.mode === 'new' ? this.name.trim().slice(0, 30) : 'Planning poker',
    });
  }

  /** Creates the game as the current (anonymous) user, who becomes its host. */
  private async createGame(): Promise<string> {
    await this.ensureUser();
    const created = await firstValueFrom(this.apollo.mutate<Mutation>({
      mutation: CREATE_GAME,
      variables: { input: { name: this.name.trim(), description: '' } }
    }));
    const game = created.data?.createGame?.game as { id: string; settingsDetails?: { id?: string } } | undefined;
    if (!game?.id) throw new Error('createGame failed');
    const settingsId = game.settingsDetails?.id;
    if (settingsId && this.deck !== DEFAULT_DECK) {
      await firstValueFrom(this.apollo.mutate({
        mutation: UPDATE_SETTINGS,
        variables: { input: { id: settingsId, gameId: game.id, deckSetup: this.deck } }
      }));
    }
    return game.id;
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

let sdk: Promise<TeamsSdk> | undefined;

function loadTeamsSdk(): Promise<TeamsSdk> {
  sdk ??= new Promise<TeamsSdk>((resolve, reject) => {
    const script = document.createElement('script');
    script.src = TEAMS_SDK_URL;
    script.crossOrigin = 'anonymous';
    script.onload = () => {
      const teams = (window as unknown as { microsoftTeams?: TeamsSdk }).microsoftTeams;
      teams ? resolve(teams) : reject(new Error('Teams SDK missing'));
    };
    script.onerror = () => reject(new Error('Teams SDK failed to load'));
    document.head.appendChild(script);
  });
  return sdk;
}
