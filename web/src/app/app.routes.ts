import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'jobs' },
  {
    path: 'jobs',
    title: 'Jobs · SwiftJob',
    loadComponent: () =>
      import('./features/jobs/dashboard/job-dashboard').then((m) => m.JobDashboard),
  },
  { path: '**', redirectTo: 'jobs' },
];
