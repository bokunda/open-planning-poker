import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { JoinGameDialogComponent } from './dialog/join-game-dialog.component';

@Component({
  selector: 'app-join-game',
  templateUrl: './join-game.component.html',
  styleUrl: './join-game.component.scss',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
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
