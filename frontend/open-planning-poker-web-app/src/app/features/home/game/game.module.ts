import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameComponent } from './game.component';
import { CreateGameComponent } from './create/create-game.component';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { OptionsComponent } from './options/options.component';
import { CreateGameDialogComponent } from './create/dialog/create-game-dialog.component';
import { JoinGameDialogComponent } from './join/dialog/join-game-dialog.component';
import { JoinGameComponent } from './join/join-game.component';
import { GameDetailsComponent } from './details/game-details.component';
import { PlayersComponent } from './players/players.component';
import { VotingComponent } from './voting/voting.component';
import { VotingHistoryComponent } from './voting-history/voting-history.component';
import { BreadcrumbComponent } from './breadcrumb/breadcrumb.component';
import { ChatComponent } from './chat/chat.component';
import { FormsModule } from '@angular/forms';



@NgModule({
  declarations: [
    OptionsComponent,
    CreateGameComponent,
    CreateGameDialogComponent,
    JoinGameComponent,
    JoinGameDialogComponent,
    GameDetailsComponent,
    GameComponent,
    VotingComponent,
    VotingHistoryComponent,
    PlayersComponent,
    BreadcrumbComponent,
    ChatComponent
  ],
  imports: [
    CommonModule,
    SharedModule,
    RouterModule,
    FormsModule
  ],
  exports: [
    GameComponent,
    BreadcrumbComponent
  ]
})
export class GameModule { }
