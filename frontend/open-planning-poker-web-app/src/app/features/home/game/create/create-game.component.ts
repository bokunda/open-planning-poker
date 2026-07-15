import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { CreateGameDialogComponent } from './dialog/create-game-dialog.component';
import { DECK_PRESETS, DEFAULT_DECK } from '../../../../shared/deck-presets';

export interface CreateGameResult {
  name: string;
  description: string;
  deckSetup: string;
}

@Component({
  selector: 'app-create-game',
  templateUrl: './create-game.component.html',
  styleUrl: './create-game.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateGameComponent {

  createGameForm: FormGroup;
  deckPresets = DECK_PRESETS;

  constructor(
    private dialogRef: MatDialogRef<CreateGameDialogComponent>,
    private fb: FormBuilder
  ) {
    this.createGameForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(5000)]],
      deckSetup: [DEFAULT_DECK, [Validators.required]]
    });
  }

  onSubmit(): void {
    if (this.createGameForm.valid) {
      this.dialogRef.close(this.createGameForm.value as CreateGameResult);
    }
  }

}
