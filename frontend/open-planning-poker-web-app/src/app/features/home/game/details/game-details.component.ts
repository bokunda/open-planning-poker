import { ChangeDetectionStrategy, Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Game } from '../../../../graphql/graphql-gateway.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { RouterLink } from '@angular/router';
import { OPP_SNACKBAR_DURATION_DEFAULT, OPP_SNACKBAR_MODAL_LABEL_CLOSE } from '../../../../shared/constants';
import { isAsyncVotingOpen, RoomSettings } from '../room-types';

/** Room options the host changes from the settings menu. */
export interface RoomOptions {
  autoReveal: boolean;
  /** ISO time the async voting window ends; null for meeting mode. */
  asyncVotingEndsAt: string | null;
}

/** How long the async voting window stays open. */
export const ASYNC_WINDOWS = [
  { label: '24 hours', hours: 24 },
  { label: '3 days', hours: 72 },
  { label: '1 week', hours: 168 },
] as const;

@Component({
  selector: 'app-game-details',
  templateUrl: './game-details.component.html',
  styleUrl: './game-details.component.scss',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatTooltipModule, MatMenuModule, MatDividerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameDetailsComponent {
  @Input() game: Game | undefined;
  @Input() isHost = false;
  @Input() settings: RoomSettings | undefined;

  @Output() onLeaveGameClick = new EventEmitter<void>();
  @Output() onGameReportClick = new EventEmitter<void>();
  @Output() onCreateNewTicket = new EventEmitter<void>();
  @Output() onExportCsv = new EventEmitter<void>();
  @Output() onUpdateOptions = new EventEmitter<RoomOptions>();

  readonly asyncWindows = ASYNC_WINDOWS;

  get teamId(): string | null {
    return (this.game as { teamId?: string | null } | undefined)?.teamId ?? null;
  }

  get autoReveal(): boolean {
    return !!this.settings?.autoReveal;
  }

  get asyncOpen(): boolean {
    return isAsyncVotingOpen(this.settings);
  }

  get asyncEndsAt(): Date | null {
    return this.asyncOpen ? new Date(this.settings!.asyncVotingEndsAt!) : null;
  }

  toggleAutoReveal(): void {
    this.onUpdateOptions.emit({ autoReveal: !this.autoReveal, asyncVotingEndsAt: this.settings?.asyncVotingEndsAt ?? null });
  }

  startAsyncVoting(hours: number): void {
    const endsAt = new Date(Date.now() + hours * 3600_000).toISOString();
    this.onUpdateOptions.emit({ autoReveal: this.autoReveal, asyncVotingEndsAt: endsAt });
  }

  endAsyncVoting(): void {
    this.onUpdateOptions.emit({ autoReveal: this.autoReveal, asyncVotingEndsAt: null });
  }

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
