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
      {
        path: 'orders',
        loadComponent: () => import('./orders/orders-page').then(m => m.OrdersPage),
        title: 'Orders · Bookstore Admin',
      },
      {
        path: 'institutional-orders/1042/shipments',
        loadComponent: () => import('./shipments/shipment-plan-page').then(m => m.ShipmentPlanPage),
        title: 'Shipments · Bookstore Admin',
      },
      {
        path: 'institutional-orders/new',
        // Forms, autocomplete and datepicker load only with this page
        loadComponent: () => import('./institutional-order/institutional-order-page').then(m => m.InstitutionalOrderPage),
        title: 'New institutional order · Bookstore Admin',
      },
    ],
  },
];
