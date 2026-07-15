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
  isCustomDeck = false;
  customDeckPreview: string[] = [];

  constructor(
    private dialogRef: MatDialogRef<CreateGameDialogComponent>,
    private fb: FormBuilder
  ) {
    this.createGameForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(5000)]],
      deckSetup: [DEFAULT_DECK, [Validators.required]],
      customDeck: ['']
    });

    this.createGameForm.get('deckSetup')?.valueChanges.subscribe(value => {
      this.isCustomDeck = value === '__custom__';
      if (this.isCustomDeck) {
        this.onCustomDeckChange();
      }
    });

    this.createGameForm.get('customDeck')?.valueChanges.subscribe(() => {
      this.onCustomDeckChange();
    });
  }

  onCustomDeckChange(): void {
    const raw = this.createGameForm.get('customDeck')?.value ?? '';
    this.customDeckPreview = raw
      .split(',')
      .map((s: string) => s.trim())
      .filter((s: string) => s.length > 0);
  }

  onSubmit(): void {
    if (this.createGameForm.valid) {
      const formValue = this.createGameForm.value;
      const result: CreateGameResult = {
        name: formValue.name,
        description: formValue.description,
        deckSetup: this.isCustomDeck ? formValue.customDeck : formValue.deckSetup
      };
      this.dialogRef.close(result);
    }
  }

}
