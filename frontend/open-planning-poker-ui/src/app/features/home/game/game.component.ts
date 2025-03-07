import { Component, inject } from '@angular/core';
import { Apollo } from 'apollo-angular';
import { Game, Mutation, MutationCreateGameArgs, Query } from '../../../graphql/graphql-gateway.service';
import { GET_GAME } from './gql/getGame.graphql';
import { CREATE_GAME } from './gql/createGame.graphql';
import { MatDialog } from '@angular/material/dialog';
import { CreateGameDialogComponent } from './create/dialog/create-game-dialog.component';
import { JoinGameDialogComponent } from './join/dialog/join-game-dialog.component';

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrl: './game.component.scss',
  standalone: false
})
export class GameComponent {

  game: Game | undefined;

  readonly dialog = inject(MatDialog);

  constructor(private apollo: Apollo) {}

  handleCreateGame() {
    this.openCreateGameDialog();
  }

  handleJoinGame() {
    this.openJoinGameDialog();
  }

  handleLeaveGame() {
    this.game = undefined;
  }

  private getGame(id: string): void {
    this.apollo.watchQuery<{ game: Game }>({
      query: GET_GAME,
      variables: { id }
    })
    .valueChanges
    .subscribe({
      next: ({ data }) => {
        if (data) {
          this.game = data?.game;
        }
      },
      error: (error) => {
        console.error('[GameComponent] Error getting game', error);
      },
      complete: () => {
        console.log('[GameComponent] Get game request completed');
      }
    });
  }

  private createGame(name: string, description: string): void {
    this.apollo.mutate<Mutation, MutationCreateGameArgs>({
      mutation: CREATE_GAME,
      variables: {input: {name, description}}
    }).subscribe({
      next: ({ data }) => {
        if (data?.createGame?.game) {
          this.game = data?.createGame?.game;
        }
      },
      error: (error) => {
        console.error('[GameComponent] Error creating a game', error);
      },
      complete: () => {
        console.log('[GameComponent] Game Creation completed');
      }
    });
  }

  private openCreateGameDialog() {
    const dialogRef = this.dialog.open(CreateGameDialogComponent, {
      width: '400px',
      maxWidth: '400px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: Game | undefined) => {
      if (!result) { return; }
      this.createGame(result.name, result.description);
    });
  }

  private openJoinGameDialog() {
    const dialogRef = this.dialog.open(JoinGameDialogComponent, {
      width: '400px',
      maxWidth: '400px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: Game | undefined) => {
      if (!result) { return; }
      this.getGame(result.id);
    });
  }

}
