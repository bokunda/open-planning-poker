import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from './material.module';
import { LoadingComponent } from './loading/loading.component';
import { QrShareDialogComponent } from './dialogs/qr-share/qr-share-dialog.component';

@NgModule({
  declarations: [LoadingComponent, QrShareDialogComponent],
  imports: [
    CommonModule,
    MaterialModule
  ],
  exports: [
    MaterialModule,
    LoadingComponent,
    QrShareDialogComponent
  ]
})
export class SharedModule { }
