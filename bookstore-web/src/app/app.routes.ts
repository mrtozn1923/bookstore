import { provideHttpClient, withXhr } from '@angular/common/http';
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./catalog/catalog-page').then((m) => m.CatalogPage) },
  { path: 'books/:isbn', loadComponent: () => import('./books/book-page').then((m) => m.BookPage) },
  {
    path: 'books/:isbn/preview',
    loadComponent: () => import('./books/preview-page').then((m) => m.PreviewPage),
  },
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
  { path: 'search', loadComponent: () => import('./search/search-page').then((m) => m.SearchPage) },
  {
    path: 'institution/delivery',
    loadComponent: () => import('./institution/delivery-page').then((m) => m.DeliveryPage),
  },
  { path: 'orders', loadComponent: () => import('./orders/orders-page').then((m) => m.OrdersPage) },
  {
    path: 'settings',
    loadComponent: () => import('./settings/settings-page').then((m) => m.SettingsPage),
  },
];
