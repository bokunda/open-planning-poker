import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from '../../shared/shared.module';
import { RouterModule } from '@angular/router';
import { HomeComponent } from './home.component';
import { HeaderComponent } from './header/header.component';
import { UserComponent } from './user/user.component';
import { GameComponent } from './game/game.component';
import { OptionsComponent } from './game/options/options.component';

@NgModule({
  declarations: [
    HeaderComponent,
    UserComponent,
    GameComponent,
    OptionsComponent,
    HomeComponent
  ],
  imports: [
    CommonModule,
    SharedModule,
    RouterModule
  ],
  exports: [
    HomeComponent
  ]
})
export class HomeModule { }
