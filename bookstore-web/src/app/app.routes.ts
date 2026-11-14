import { Routes } from '@angular/router';
import { HomePage } from './home/home-page';

export const routes: Routes = [
  // The home page (LCP page, prerendered) stays in the initial bundle
  { path: '', component: HomePage, title: 'Bookstore' },
  // Lazy routes: each page and what only it imports (the CDK for the catalog) get their own chunk
  { path: 'books', loadComponent: () => import('./catalog/catalog-page').then((m) => m.CatalogPage), title: 'Catalog - Bookstore', data: { preload: true } },
  { path: 'books/:isbn', loadComponent: () => import('./catalog/book-page').then((m) => m.BookPage), title: 'Book - Bookstore', data: { preload: true } },
  // Back office: a few staff users, none of its code (or chart.js) belongs to a customer's first load
  { path: 'admin/reports', loadComponent: () => import('./admin/reports-page').then((m) => m.ReportsPage), title: 'Reports - Bookstore' },
];
