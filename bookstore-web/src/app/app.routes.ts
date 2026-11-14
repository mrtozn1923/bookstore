import { provideHttpClient, withXhr } from '@angular/common/http';
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./catalog/catalog-page').then((m) => m.CatalogPage) },
  {
    path: 'reports/orders',
    loadComponent: () => import('./reports/order-report-page').then((m) => m.OrderReportPage),
  },
  {
    path: 'admin/covers',
    loadComponent: () => import('./admin/cover-upload-page').then((m) => m.CoverUploadPage),
    // only this route uses XMLHttpRequest, for upload progress; the rest of the app keeps Fetch
    providers: [provideHttpClient(withXhr())],
  },
  {
    path: 'settings',
    loadComponent: () => import('./settings/settings-page').then((m) => m.SettingsPage),
  },
];
