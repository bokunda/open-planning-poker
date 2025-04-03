import { ChangeDetectionStrategy, Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { Game } from '../../../../graphql/graphql-gateway.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OPP_SNACKBAR_DURATION_DEFAULT, OPP_SNACKBAR_MODAL_LABEL_CLOSE } from '../../../../shared/constants';

@Component({
  selector: 'app-game-details',
  templateUrl: './game-details.component.html',
  styleUrl: './game-details.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameDetailsComponent {
  @Input() game: Game | undefined;

  @Output() onLeaveGameClick = new EventEmitter<void>();

  private snackBar = inject(MatSnackBar);

  copyToClipboard(): void {
    navigator.clipboard
      .writeText(window.location.href)
      .then(() => {
        this.snackBar.open('Game Id copied to clipboard successfully!', OPP_SNACKBAR_MODAL_LABEL_CLOSE, {
          duration: OPP_SNACKBAR_DURATION_DEFAULT,
        });
      })
      .catch((err) => {
        this.snackBar.open('Could not copy text to clipboard!');
        console.error('Could not copy text: ', err);
      });
  }
}
