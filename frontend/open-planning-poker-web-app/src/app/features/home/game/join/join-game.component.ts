import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { JoinGameDialogComponent } from './dialog/join-game-dialog.component';

import { extractGameId } from './extract-game-id';
import { TranslocoPipe } from '@jsverse/transloco';

export { extractGameId };

@Component({
  selector: 'app-join-game',
  templateUrl: './join-game.component.html',
  styleUrl: './join-game.component.scss',
  standalone: true,
  imports: [TranslocoPipe, CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JoinGameComponent {

  joinGameForm: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<JoinGameDialogComponent>,
    private fb: FormBuilder
  ) {
    this.joinGameForm = this.fb.group({
      id: [''],
    });
  }

  onSubmit(): void {
    const rawId = this.joinGameForm.get('id')?.value ?? '';
    const gameId = extractGameId(rawId);
    if (gameId.length === 36) {
      this.dialogRef.close({ id: gameId });
    } else {
      this.joinGameForm.get('id')?.setErrors({ invalidGameId: true });
    }
  }
}
