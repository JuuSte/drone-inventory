import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'drones' },
  {
    path: 'drones',
    title: 'Drones',
    loadComponent: () => import('./pages/drones/drones').then((m) => m.DronesPage)
  },
  {
    path: 'student-groups',
    title: 'Student Groups',
    loadComponent: () => import('./pages/student-groups/student-groups').then((m) => m.StudentGroupsPage)
  },
  {
    path: 'loans',
    title: 'Loans',
    loadComponent: () => import('./pages/loans/loans').then((m) => m.LoansPage)
  },
  {
    path: 'history',
    title: 'History',
    loadComponent: () => import('./pages/history/history').then((m) => m.HistoryPage)
  },
  { path: '**', redirectTo: 'drones' }
];
