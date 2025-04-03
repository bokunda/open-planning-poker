import { Component, inject, Input } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ChangeUsernameInput, Game, Mutation } from '../../../graphql/graphql-gateway.service';
import { UpdateUserDialogComponent } from './update/dialog/update-user-dialog.component';
import { Apollo } from 'apollo-angular';
import { CHANGE_USERNAME } from './gql/changeUsername.graphql';
import { GET_USER } from './gql/getUser.graphql';

@Component({
  selector: 'app-user',
  templateUrl: './user.component.html',
  styleUrl: './user.component.scss',
  standalone: false
})
export class UserComponent {
  @Input() username: string = '';

  readonly dialog = inject(MatDialog);

  constructor(private apollo: Apollo) { }

  handleUpdateUser(): void {
    this.openUpdateUserDialog();
  }

  private openUpdateUserDialog() {
    const dialogRef = this.dialog.open(UpdateUserDialogComponent, {
      width: '400px',
      maxWidth: '400px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: ChangeUsernameInput | undefined) => {
      if (!result) { return; }
      this.changeUsername(result.username);
    });
  }

  private changeUsername(username: string): void {
    this.apollo.mutate<Mutation, ChangeUsernameInput>({
      mutation: CHANGE_USERNAME,
      variables: { username },
      refetchQueries: [{ query: GET_USER }]
    }).subscribe({
      next: ({ data }) => {
        if (!!data?.changeUsername?.boolean) {
          this.username = username;
        }
      }
    });
  }
}
