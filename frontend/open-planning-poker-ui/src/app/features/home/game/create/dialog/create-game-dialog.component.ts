import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-create-game-dialog',
  templateUrl: './create-game-dialog.component.html',
  styleUrl: './create-game-dialog.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateGameDialogComponent { }
