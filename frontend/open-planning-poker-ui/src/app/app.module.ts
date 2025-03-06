import { NgModule } from '@angular/core';
import { AppComponent } from './app.component';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HomeModule } from './features/home/home.module';
import { SharedModule } from './shared/shared.module';
import { ErrorsModule } from './features/errors/errors.module';
import { RouterModule } from '@angular/router';
import { routes } from './app.routes';
import { appConfig } from './app.config';
import { HttpClientModule } from '@angular/common/http';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    RouterModule.forRoot(routes),
    BrowserAnimationsModule,
    RouterModule,
    HomeModule,
    SharedModule,
    ErrorsModule
  ],
  bootstrap: [AppComponent],
  providers: [...appConfig.providers ]
})
export class AppModule { }
