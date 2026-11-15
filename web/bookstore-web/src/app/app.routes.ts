import { Routes } from '@angular/router';
import { catalogQueryGuard } from './catalog/catalog.guard';

const catalog = () => import('./catalog/catalog-page').then((m) => m.CatalogPage);

export const routes: Routes = [
  { path: 'books/genre/:genre', canActivate: [catalogQueryGuard], loadComponent: catalog },
  { path: 'books/genre/:genre/:language', canActivate: [catalogQueryGuard], loadComponent: catalog },
  { path: 'books/:slug', loadComponent: () => import('./books/book-detail').then((m) => m.BookDetail) },
];
