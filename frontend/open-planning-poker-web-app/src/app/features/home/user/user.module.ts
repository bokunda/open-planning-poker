import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserComponent } from './user.component';
import { UpdateUserComponent } from './update/update-user.component';
import { UpdateUserDialogComponent } from './update/dialog/update-user-dialog.component';
import { SharedModule } from '../../../shared/shared.module';



@NgModule({
  declarations: [
    UserComponent,
    UpdateUserComponent,
    UpdateUserDialogComponent
  ],
  imports: [
    CommonModule,
    SharedModule
  ],
  exports: [UserComponent]
})
export class UserModule { }
