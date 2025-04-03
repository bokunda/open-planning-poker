import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { JoinGameDialogComponent } from './dialog/join-game-dialog.component';

@Component({
  selector: 'app-join-game',
  templateUrl: './join-game.component.html',
  styleUrl: './join-game.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JoinGameComponent {

  joinGameForm: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<JoinGameDialogComponent>,
    private fb: FormBuilder
  ) {
    this.joinGameForm = this.fb.group({
      id: ['', [Validators.required, Validators.minLength(36), Validators.maxLength(36)]],
    });
  }

  onSubmit(): void {
    if (this.joinGameForm.valid) {
      this.dialogRef.close(this.joinGameForm.value);
    }
  }
}
