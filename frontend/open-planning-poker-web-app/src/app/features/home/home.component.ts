import { Apollo } from 'apollo-angular';
import { Component, inject, OnInit } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { Mutation, Query, RegisterUserInput, User } from '../../graphql/graphql-gateway.service';
import { GET_USER } from './user/gql/getUser.graphql';
import { REGISTER_USER } from './user/gql/registerUser.graphql';
import { BreadcrumbItem } from './game/breadcrumb/breadcrumb.component';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  standalone: false
})
export class HomeComponent implements OnInit {

  username: string = '';
  userId: string = '';
  breadcrumbItems: BreadcrumbItem[] = [];

  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly route = inject(ActivatedRoute);

  constructor(private apollo: Apollo) { }

  ngOnInit(): void {
    this.setupUser();
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

    if (gameId && ticketId) {
      this.title.setTitle('Planning Poker — Voting Session | Open Planning Poker');
      this.meta.updateTag({ name: 'description', content: 'Vote on user stories in this planning poker session. Join your team and estimate collaboratively.' });
    } else if (gameId) {
      this.title.setTitle('Planning Poker — Game Room | Open Planning Poker');
      this.meta.updateTag({ name: 'description', content: 'Join this planning poker game room. Estimate user stories with your agile team in real-time.' });
    } else {
      this.title.setTitle('Open Planning Poker — Free Agile Estimation Tool');
      this.meta.updateTag({ name: 'description', content: 'Play Planning Poker online for free. Create a game room, invite your team, and estimate user stories collaboratively.' });
    }
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
          localStorage.setItem('token', data.registerUser.registerUserResponse.token);
          this.username = data.registerUser.registerUserResponse.userName;
        }
      }
    });
  }

}
