import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from '../../shared/shared.module';
import { RouterModule } from '@angular/router';
import { HomeComponent } from './home.component';
import { HeaderComponent } from './header/header.component';
import { UserComponent } from './user/user.component';
import { GameModule } from './game/game.module';
import { UserModule } from './user/user.module';
import { TicketModule } from './game/ticket/ticket.module';

@NgModule({
  declarations: [
    HeaderComponent,
    HomeComponent
  ],
  imports: [
    CommonModule,
    SharedModule,
    GameModule,
    TicketModule,
    UserModule,
    RouterModule
  ],
  exports: [
    HomeComponent
  ]
})
export class HomeModule { }
