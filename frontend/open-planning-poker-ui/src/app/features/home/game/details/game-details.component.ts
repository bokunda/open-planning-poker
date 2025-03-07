import { ChangeDetectionStrategy, Component, inject, Input } from '@angular/core';
import { Game } from '../../../../graphql/graphql-gateway.service';
import { MatSnackBar, MatSnackBarRef } from '@angular/material/snack-bar';

@Component({
  selector: 'app-game-details',
  templateUrl: './game-details.component.html',
  styleUrl: './game-details.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameDetailsComponent {
  @Input() game: Game | undefined;

  private snackBar = inject(MatSnackBar);

  copyToClipboard(text: string): void {
    navigator.clipboard
      .writeText(text)
      .then(() => {
        this.snackBar.open('Game Id copied to clipboard successfully!');
      })
      .catch((err) => {
        this.snackBar.open('Could not copy text to clipboard!');
        console.error('Could not copy text: ', err);
      });
  }
}
