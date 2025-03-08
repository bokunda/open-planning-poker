import { Apollo } from 'apollo-angular';
import { Component, OnInit } from '@angular/core';
import { Mutation, Query, RegisterUserInput } from '../../graphql/graphql-gateway.service';
import { GET_USER } from './user/gql/getUser.graphql';
import { REGISTER_USER } from './user/gql/registerUser.graphql';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  standalone: false
})
export class HomeComponent implements OnInit {

  username: string = '';

  constructor(private apollo: Apollo) { }

  ngOnInit(): void {
    this.setupUser();
  }

  public userExists(): boolean {
    return this.username !== '';
  }

  private setupUser() {

    this.apollo.watchQuery<Query>({query: GET_USER})
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        if (data) {
          this.username = data.currentUser.userName;
        }
        else
        {
          this.registerUser();
        }
      },
      error: (error) => {
        console.error('[HomeComponent] Error getting user', error);
        this.registerUser();
      },
      complete: () => {
        console.log('[HomeComponent] Get user request completed');
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
          console.log('[HomeComponent] New user created', JSON.stringify(data, null, 2));
          localStorage.setItem('token', data.registerUser.registerUserResponse.token);
          this.username = data.registerUser.registerUserResponse.userName;
        }
      },
      error: (error) => {
        console.error('[HomeComponent] Error registering user', error);
      },
      complete: () => {
        console.log('[HomeComponent] Registration request completed');
      }
    });
  }

}
