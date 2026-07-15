import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatDialogModule } from '@angular/material/dialog';
import { CreateTicketComponent } from '../create-ticket.component';

@Component({
  selector: 'app-create-ticket-dialog',
  templateUrl: './create-ticket-dialog.component.html',
  styleUrl: './create-ticket-dialog.component.scss',
  standalone: true,
  imports: [MatDialogModule, CreateTicketComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateTicketDialogComponent {}
