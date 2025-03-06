import { Apollo } from 'apollo-angular';
import { Component, OnInit } from '@angular/core';
import {
  //GET_USER,
  REGISTER_USER
} from './user.graphql.operations';

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
    if (!this.getUser()) {
      this.registerUser();
    }
  }

  private getUser(): boolean {
    return false;
  }

  private ping(): void {

  }

  private registerUser(): void {
    this.apollo.mutate<any, any>({
      mutation: REGISTER_USER,
      variables: {},
      //refetchQueries: [{query: GET_USER}]
    }).subscribe({
      next: ({ data }) => {
        if (data) {
          console.log('[HomeComponent] New user created', JSON.stringify(data, null, 2));
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
