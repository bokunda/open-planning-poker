import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MarkdownPipe } from '../../../../../shared/markdown/markdown.pipe';

export interface EditTicketResult {
  name: string;
  description: string;
}

/** Edits a ticket's title and description. The description supports a small Markdown subset. */
@Component({
  selector: 'app-edit-ticket-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MarkdownPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './edit-ticket-dialog.component.html',
  styles: [`
    .edit-ticket { display: grid; gap: var(--opp-space-2); padding-top: var(--opp-space-2); }
    .edit-ticket mat-form-field { width: 100%; }
    .edit-preview { border: 1px dashed var(--opp-border); border-radius: var(--opp-radius-md); padding: var(--opp-space-3); }
    .edit-preview-label {
      display: block; margin-bottom: var(--opp-space-2);
      font-size: var(--opp-font-size-xs); font-weight: 600; letter-spacing: .06em; text-transform: uppercase;
      color: var(--opp-text-muted);
    }
  `]
})
export class EditTicketDialogComponent {
  private readonly data = inject<EditTicketResult>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<EditTicketDialogComponent>);

  readonly form = inject(FormBuilder).nonNullable.group({
    name: [this.data.name, [Validators.required, Validators.minLength(3), Validators.maxLength(255)]],
    description: [this.data.description ?? '', [Validators.maxLength(4080)]],
  });

  save(): void {
    if (this.form.invalid) return;
    const { name, description } = this.form.getRawValue();
    this.dialogRef.close({ name: name.trim(), description: description.trim() } satisfies EditTicketResult);
  }
}
