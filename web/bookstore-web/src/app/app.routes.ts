import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: 'books', loadComponent: () => import('./book-list').then((m) => m.BookList) },
];
