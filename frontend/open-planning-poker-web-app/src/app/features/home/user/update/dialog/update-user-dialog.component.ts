import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-update-user-dialog',
  templateUrl: './update-user-dialog.component.html',
  styleUrl: './update-user-dialog.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UpdateUserDialogComponent {

}
