import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { TranslocoPipe } from '@jsverse/transloco';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel: string;
  /** Red confirm button for actions that delete or remove something. */
  destructive?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [TranslocoPipe, MatDialogModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content><p class="confirm-message">{{ data.message }}</p></mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{ 'common.cancel' | transloco }}</button>
      <button mat-flat-button color="primary" [class.confirm-danger]="data.destructive" [mat-dialog-close]="true" cdkFocusInitial>
        {{ data.confirmLabel }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .confirm-message { margin: 0; color: var(--opp-text-secondary); line-height: 1.5; }
    .confirm-danger {
      --mdc-filled-button-container-color: var(--opp-danger, #b3261e);
      --mdc-filled-button-label-text-color: #fff;
    }
  `]
})
export class ConfirmDialogComponent {
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
