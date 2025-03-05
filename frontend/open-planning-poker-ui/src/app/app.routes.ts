import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { ErrorsComponent } from './features/errors/errors.component';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent,
    pathMatch: 'full'
  },
  {
    path: '**',
    component: ErrorsComponent,
  }
];
