import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { UpdateUserDialogComponent } from './dialog/update-user-dialog.component';

@Component({
  selector: 'app-update-user',
  templateUrl: './update-user.component.html',
  styleUrl: './update-user.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UpdateUserComponent {

  @Input() username: string | undefined;

  updateUserForm: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<UpdateUserDialogComponent>,
    private fb: FormBuilder
  ) {
    this.updateUserForm = this.fb.group({
      username: [this.username, [Validators.required, Validators.minLength(3), Validators.maxLength(36)]],
    });
  }

  onSubmit(): void {
    if (this.updateUserForm.valid) {
      this.dialogRef.close(this.updateUserForm.value);
    }
  }
}
