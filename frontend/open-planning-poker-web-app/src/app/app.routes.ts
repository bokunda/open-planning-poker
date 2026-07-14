import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { ErrorsComponent } from './features/errors/errors.component';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent,
    pathMatch: 'full',
    title: 'Open Planning Poker — Free Agile Estimation Tool'
  },
  {
    path: 'game',
    component: HomeComponent,
    pathMatch: 'full',
    title: 'Create or Join a Game — Open Planning Poker'
  },
  {
    path: 'game/:id',
    component: HomeComponent,
    pathMatch: 'full',
    title: 'Planning Poker — Game Room | Open Planning Poker'
  },
  {
    path: 'game/:id/ticket/:ticketId',
    component: HomeComponent,
    pathMatch: 'full',
    title: 'Planning Poker — Voting Session | Open Planning Poker'
  },
  {
    path: '**',
    component: ErrorsComponent,
    title: '404 — Page Not Found | Open Planning Poker'
  }
];
