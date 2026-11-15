import { Routes } from '@angular/router';
import { AdminShell } from './admin-shell';
import { DashboardPage } from './dashboard-page';

export const adminRoutes: Routes = [
  {
    path: '',
    component: AdminShell,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardPage, title: 'Dashboard · Bookstore Admin' },
    ],
  },
];
