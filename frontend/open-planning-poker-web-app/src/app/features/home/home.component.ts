import { Apollo } from 'apollo-angular';
import { Component, inject, OnInit, afterNextRender, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Title, Meta } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { Mutation, Query, RegisterUserInput, User } from '../../graphql/graphql-gateway.service';
import { GET_USER } from './user/gql/getUser.graphql';
import { REGISTER_USER } from './user/gql/registerUser.graphql';
import { BreadcrumbItem } from './game/breadcrumb/breadcrumb.component';
import { HeaderComponent } from './header/header.component';
import { BreadcrumbComponent } from './game/breadcrumb/breadcrumb.component';
import { GameComponent } from './game/game.component';
import { FooterComponent } from './footer/footer.component';
import { TeamComponent } from './team/team.component';
import { TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  standalone: true,
  imports: [CommonModule, HeaderComponent, BreadcrumbComponent, GameComponent, TeamComponent, FooterComponent]
})
export class HomeComponent implements OnInit {

  username: string = '';
  userId: string = '';
  breadcrumbItems: BreadcrumbItem[] = [];
  /** /team and /team/:teamId show the team rooms instead of the game. */
  isTeamView = false;

  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly route = inject(ActivatedRoute);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly i18n = inject(TranslocoService);

  constructor(private apollo: Apollo) {
    // Delay user setup until after hydration to avoid SSR/client mismatch
    afterNextRender(() => {
      this.setupUser();
    });
  }

  ngOnInit(): void {
    this.isTeamView = this.route.snapshot.data['view'] === 'team';
    this.updatePageMeta();
    // Titles and descriptions follow the language.
    this.i18n.langChanges$.subscribe(() => this.updatePageMeta());
    this.route.paramMap.subscribe(params => {
      if (this.isTeamView) {
        this.updateTeamBreadcrumb(params.get('teamId'));
      } else {
        this.updateBreadcrumb(params.get('id'), params.get('ticketId'));
      }
    });
  }

  private updateTeamBreadcrumb(teamId: string | null): void {
    this.breadcrumbItems = teamId
      ? [{ label: 'nav.home', url: '/' }, { label: 'nav.teams', url: '/team' }, { label: 'nav.teamRoom' }]
      : [{ label: 'nav.home', url: '/' }, { label: 'nav.teams' }];
  }

  private updateBreadcrumb(gameId: string | null, ticketId: string | null): void {
    const items: BreadcrumbItem[] = [
      { label: 'nav.home', url: '/' }
    ];
    if (gameId) {
      items.push({ label: 'nav.game', url: `/game/${gameId}` });
    }
    if (ticketId) {
      items.push({ label: 'nav.voting' });
    }
    this.breadcrumbItems = items;
  }

  private updatePageMeta(): void {
    if (this.isTeamView) {
      this.title.setTitle(this.i18n.translate(this.route.snapshot.paramMap.get('teamId') ? 'meta.teamRoomTitle' : 'meta.teamRoomsTitle'));
      return;
    }
    const gameId = this.route.snapshot.paramMap.get('id');
    const ticketId = this.route.snapshot.paramMap.get('ticketId');
    const baseUrl = 'https://app.openplanningpoker.com';
    let pageUrl = baseUrl;
    let pageTitle = this.i18n.translate('meta.homeTitle');
    let pageDesc = this.i18n.translate('meta.homeDescription');

    if (gameId && ticketId) {
      pageUrl = `${baseUrl}/game/${gameId}/ticket/${ticketId}`;
      pageTitle = this.i18n.translate('meta.votingTitle');
      pageDesc = this.i18n.translate('meta.votingDescription');
    } else if (gameId) {
      pageUrl = `${baseUrl}/game/${gameId}`;
      pageTitle = this.i18n.translate('meta.roomTitle');
      pageDesc = this.i18n.translate('meta.roomDescription');
    }

    this.title.setTitle(pageTitle);
    this.meta.updateTag({ name: 'description', content: pageDesc });
    this.meta.updateTag({ rel: 'canonical', href: pageUrl });
    this.meta.updateTag({ property: 'og:title', content: pageTitle });
    this.meta.updateTag({ property: 'og:description', content: pageDesc });
    this.meta.updateTag({ property: 'og:url', content: pageUrl });
    this.meta.updateTag({ name: 'twitter:title', content: pageTitle });
    this.meta.updateTag({ name: 'twitter:description', content: pageDesc });
  }

  public userExists(): boolean {
    return this.username !== '';
  }

  private setupUser() {

    this.apollo.watchQuery<Query>({query: GET_USER})
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        let currentUser = data?.currentUser as User;
        if (currentUser?.userName) {
          this.username = currentUser.userName;
          this.userId = currentUser.id;
        }
        else
        {
          this.registerUser();
        }
      },
      error: ({ error }) => {
        this.registerUser();
      }
    });
  }

  private registerUser(): void {
    this.apollo.mutate<Mutation, RegisterUserInput>({
      mutation: REGISTER_USER,
      variables: {},
      refetchQueries: [{ query: GET_USER }]
    }).subscribe({
      next: ({ data }) => {
        if (data?.registerUser?.registerUserResponse?.token) {
          if (typeof localStorage !== 'undefined') {
            localStorage.setItem('token', data.registerUser.registerUserResponse.token);
          }
          this.username = data.registerUser.registerUserResponse.userName;
          this.userId = data.registerUser.registerUserResponse.id;
        }
      }
    });
  }

}
