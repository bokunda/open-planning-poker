import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CreateTicketComponent } from './create-ticket/create-ticket.component';
import { CreateTicketDialogComponent } from './create-ticket/dialog/create-ticket-dialog.component';
import { MaterialModule } from '../../../../shared/material.module';


@NgModule({
  declarations: [
    CreateTicketComponent,
    CreateTicketDialogComponent
  ],
  imports: [
    CommonModule,
    MaterialModule
  ],
  exports: [
    CreateTicketComponent
  ]
})
export class TicketModule { }
