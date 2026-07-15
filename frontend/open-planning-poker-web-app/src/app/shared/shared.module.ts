import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from './material.module';
import { LoadingComponent } from './loading/loading.component';



@NgModule({
  declarations: [LoadingComponent],
  imports: [
    CommonModule,
    MaterialModule
  ],
  exports: [
    MaterialModule,
    LoadingComponent
  ]
})
export class SharedModule { }
