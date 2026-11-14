import { Routes } from '@angular/router';
import { BookDetails } from './book-details';

export const routes: Routes = [
  { path: 'books/:isbn', component: BookDetails },
  { path: '', redirectTo: 'books/9780441172719', pathMatch: 'full' },
];
