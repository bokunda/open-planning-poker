import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';

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
    path: 'team',
    component: HomeComponent,
    pathMatch: 'full',
    data: { view: 'team' },
    title: 'Team Rooms — Open Planning Poker'
  },
  {
    path: 'team/:teamId',
    component: HomeComponent,
    pathMatch: 'full',
    data: { view: 'team' },
    title: 'Team Room — Open Planning Poker'
  },
  {
    // Public, read-only recap of a session (OPP-170).
    path: 'recap/:token',
    loadComponent: () => import('./features/recap/recap.component').then(m => m.RecapComponent),
    title: 'Session recap — Open Planning Poker'
  },
  {
    // Atlassian OAuth callback (OPP-10).
    path: 'integrations/jira/callback',
    loadComponent: () => import('./features/integrations/jira-callback.component').then(m => m.JiraCallbackComponent),
    title: 'Connecting Jira — Open Planning Poker'
  },
  {
    // GitHub, Linear and Azure DevOps OAuth callbacks (OPP-116).
    path: 'integrations/tracker/:provider/callback',
    loadComponent: () => import('./features/integrations/tracker-callback.component').then(m => m.TrackerCallbackComponent),
    title: 'Connecting — Open Planning Poker'
  },
  {
    // Started from the Jira issue panel: a game with that issue as the first ticket (OPP-120).
    path: 'estimate',
    loadComponent: () => import('./features/integrations/estimate-issue.component').then(m => m.EstimateIssueComponent),
    title: 'Estimate a Jira issue — Open Planning Poker'
  },
  {
    // Configuration page of the Microsoft Teams tab (OPP-23).
    path: 'teams/config',
    loadComponent: () => import('./features/integrations/teams-config.component').then(m => m.TeamsConfigComponent),
    title: 'Open Planning Poker for Microsoft Teams'
  },
  {
    path: '**',
    loadComponent: () => import('./features/errors/errors.component').then(m => m.ErrorsComponent),
    title: '404 — Page Not Found | Open Planning Poker'
  }
];
