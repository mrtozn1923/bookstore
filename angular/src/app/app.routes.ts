import { Routes } from '@angular/router';
import { BookPage } from './book-page';
import { CatalogPage } from './catalog-page';

export const routes: Routes = [
  { path: '', component: CatalogPage, title: 'Catalog | Bookstore' },
  { path: 'books/:slug', component: BookPage, title: 'Book | Bookstore' },
];
