import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatDialogModule } from '@angular/material/dialog';
import { UpdateUserComponent } from '../update-user.component';

@Component({
  selector: 'app-update-user-dialog',
  templateUrl: './update-user-dialog.component.html',
  styleUrl: './update-user-dialog.component.scss',
  standalone: true,
  imports: [MatDialogModule, UpdateUserComponent],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UpdateUserDialogComponent {

}
