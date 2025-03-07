import { ChangeDetectionStrategy, Component, EventEmitter, Output } from '@angular/core';
import { Game } from '../../../../graphql/graphql-gateway.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { CreateGameDialogComponent } from './dialog/create-game-dialog.component';

@Component({
  selector: 'app-create-game',
  templateUrl: './create-game.component.html',
  styleUrl: './create-game.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateGameComponent {

  createGameForm: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<CreateGameDialogComponent>,
    private fb: FormBuilder
  ) {
    this.createGameForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(5000)]]
    });
  }

  onSubmit(): void {
    if (this.createGameForm.valid) {
      this.dialogRef.close(this.createGameForm.value);
    }
  }

}
