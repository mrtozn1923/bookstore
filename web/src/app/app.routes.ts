import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./assistant/assistant').then((m) => m.Assistant) },
  { path: 'books/:isbn', loadComponent: () => import('./books/book-detail').then((m) => m.BookDetail) },
];
