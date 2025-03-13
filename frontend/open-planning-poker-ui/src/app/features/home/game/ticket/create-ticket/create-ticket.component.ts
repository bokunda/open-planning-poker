import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CreateTicketDialogComponent } from './dialog/create-ticket-dialog.component';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-create-ticket',
  templateUrl: './create-ticket.component.html',
  styleUrl: './create-ticket.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateTicketComponent {
  createTicketForm: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<CreateTicketDialogComponent>,
    private fb: FormBuilder
  ) {
    this.createTicketForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(5000)]]
    });
  }

  onSubmit(): void {
    if (this.createTicketForm.valid) {
      this.dialogRef.close(this.createTicketForm.value);
    }
  }
}
