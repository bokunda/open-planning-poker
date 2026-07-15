import { Routes } from '@angular/router';
import { ErrorsComponent } from './features/errors/errors.component';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./features/home/home.module').then(m => m.HomeModule),
    pathMatch: 'full',
    title: 'Open Planning Poker — Free Agile Estimation Tool'
  },
  {
    path: 'game',
    loadChildren: () => import('./features/home/home.module').then(m => m.HomeModule),
    pathMatch: 'full',
    title: 'Create or Join a Game — Open Planning Poker'
  },
  {
    path: 'game/:id',
    loadChildren: () => import('./features/home/home.module').then(m => m.HomeModule),
    pathMatch: 'full',
    title: 'Planning Poker — Game Room | Open Planning Poker'
  },
  {
    path: 'game/:id/ticket/:ticketId',
    loadChildren: () => import('./features/home/home.module').then(m => m.HomeModule),
    pathMatch: 'full',
    title: 'Planning Poker — Voting Session | Open Planning Poker'
  },
  {
    path: '**',
    component: ErrorsComponent,
    title: '404 — Page Not Found | Open Planning Poker'
  }
];
