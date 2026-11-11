import { ChangeDetectionStrategy, Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Game } from '../../../../graphql/graphql-gateway.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { OPP_SNACKBAR_DURATION_DEFAULT, OPP_SNACKBAR_MODAL_LABEL_CLOSE } from '../../../../shared/constants';

@Component({
  selector: 'app-game-details',
  templateUrl: './game-details.component.html',
  styleUrl: './game-details.component.scss',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule, MatMenuModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameDetailsComponent {
  @Input() game: Game | undefined;
  @Input() isHost = false;

  @Output() onLeaveGameClick = new EventEmitter<void>();
  @Output() onGameReportClick = new EventEmitter<void>();
  @Output() onCreateNewTicket = new EventEmitter<void>();

  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  async openQrShare(): Promise<void> {
    // Loaded on first open, with the QR code library.
    const { QrShareDialogComponent } = await import('../../../../shared/dialogs/qr-share/qr-share-dialog.component');
    this.dialog.open(QrShareDialogComponent, {
      width: '360px',
      maxWidth: '95vw',
      data: { url: window.location.href, gameName: this.game?.name ?? 'Game' }
    });
  }

  copyToClipboard(): void {
    navigator.clipboard
      .writeText(window.location.href)
      .then(() => {
        this.snackBar.open('Game link copied. Share it with your team.', OPP_SNACKBAR_MODAL_LABEL_CLOSE, {
          duration: OPP_SNACKBAR_DURATION_DEFAULT,
        });
      })
      .catch((err) => {
        this.snackBar.open('Could not copy text to clipboard!');
        console.error('Could not copy text: ', err);
      });
  }
}
