import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: 'books/:slug', loadComponent: () => import('./books/book-detail').then((m) => m.BookDetail) },
];
