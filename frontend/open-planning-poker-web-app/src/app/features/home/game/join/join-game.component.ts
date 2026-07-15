import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { JoinGameDialogComponent } from './dialog/join-game-dialog.component';

function extractGameId(input: string): string {
  const trimmed = input.trim();
  // Full URL: https://app.openplanningpoker.com/game/UUID or /game/UUID/ticket/UUID
  const urlMatch = trimmed.match(/\/game\/([a-f0-9]{36})/i);
  if (urlMatch) return urlMatch[1];
  // Plain UUID
  const uuidMatch = trimmed.match(/^([a-f0-9]{36})$/i);
  if (uuidMatch) return uuidMatch[1];
  // Return as-is (validator will reject if not 36 chars)
  return trimmed;
}

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
