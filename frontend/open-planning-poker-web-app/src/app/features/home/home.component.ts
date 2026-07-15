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
import { LoadingComponent } from '../../shared/loading/loading.component';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  standalone: true,
  imports: [CommonModule, HeaderComponent, BreadcrumbComponent, GameComponent, FooterComponent, LoadingComponent]
})
export class HomeComponent implements OnInit {

  username: string = '';
  userId: string = '';
  breadcrumbItems: BreadcrumbItem[] = [];

  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly route = inject(ActivatedRoute);
  private readonly platformId = inject(PLATFORM_ID);

  constructor(private apollo: Apollo) {
    // Delay user setup until after hydration to avoid SSR/client mismatch
    afterNextRender(() => {
      this.setupUser();
    });
  }

  ngOnInit(): void {
    this.updatePageMeta();
    this.route.paramMap.subscribe(params => {
      this.updateBreadcrumb(params.get('id'), params.get('ticketId'));
    });
  }

  private updateBreadcrumb(gameId: string | null, ticketId: string | null): void {
    const items: BreadcrumbItem[] = [
      { label: 'Home', url: '/' }
    ];
    if (gameId) {
      items.push({ label: 'Game', url: `/game/${gameId}` });
    }
    if (ticketId) {
      items.push({ label: 'Voting' });
    }
    this.breadcrumbItems = items;
  }

  private updatePageMeta(): void {
    const gameId = this.route.snapshot.paramMap.get('id');
    const ticketId = this.route.snapshot.paramMap.get('ticketId');
    const baseUrl = 'https://app.openplanningpoker.com';
    let pageUrl = baseUrl;
    let pageTitle = 'Open Planning Poker — Free Agile Estimation Tool';
    let pageDesc = 'Play Planning Poker online for free. Create a game room, invite your team, and estimate user stories collaboratively.';

    if (gameId && ticketId) {
      pageUrl = `${baseUrl}/game/${gameId}/ticket/${ticketId}`;
      pageTitle = 'Planning Poker — Voting Session | Open Planning Poker';
      pageDesc = 'Vote on user stories in this planning poker session. Join your team and estimate collaboratively.';
    } else if (gameId) {
      pageUrl = `${baseUrl}/game/${gameId}`;
      pageTitle = 'Planning Poker — Game Room | Open Planning Poker';
      pageDesc = 'Join this planning poker game room. Estimate user stories with your agile team in real-time.';
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
        }
      }
    });
  }

}
