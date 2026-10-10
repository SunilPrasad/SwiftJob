import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'jobs' },
  {
    path: 'jobs',
    title: 'Jobs · SwiftJob',
    loadComponent: () =>
      import('./features/jobs/dashboard/job-dashboard').then((m) => m.JobDashboard),
  },
  {
    path: 'settings',
    title: 'Settings · SwiftJob',
    loadComponent: () => import('./features/settings/settings-page').then((m) => m.SettingsPage),
  },
  {
    path: 'browser-lab',
    title: 'Browser lab · SwiftJob',
    loadComponent: () => import('./features/browser-lab/browser-lab').then((m) => m.BrowserLab),
  },
  { path: '**', redirectTo: 'jobs' },
];
