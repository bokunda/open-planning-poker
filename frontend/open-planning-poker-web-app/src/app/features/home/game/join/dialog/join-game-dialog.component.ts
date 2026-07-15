import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatDialogModule } from '@angular/material/dialog';
import { JoinGameComponent } from '../join-game.component';

@Component({
  selector: 'app-join-game-dialog',
  templateUrl: './join-game-dialog.component.html',
  styleUrl: './join-game-dialog.component.scss',
  standalone: true,
  imports: [MatDialogModule, JoinGameComponent],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JoinGameDialogComponent {

}
