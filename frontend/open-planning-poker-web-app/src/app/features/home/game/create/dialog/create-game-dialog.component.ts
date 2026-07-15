import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CreateGameComponent } from '../create-game.component';

@Component({
  selector: 'app-create-game-dialog',
  templateUrl: './create-game-dialog.component.html',
  styleUrl: './create-game-dialog.component.scss',
  standalone: true,
  imports: [MatDialogModule, CreateGameComponent],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateGameDialogComponent { }
