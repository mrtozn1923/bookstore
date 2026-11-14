import { Routes } from '@angular/router';
import { BookPage } from './catalog/book-page';
import { CatalogPage } from './catalog/catalog-page';
import { HomePage } from './home/home-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'Bookstore' },
  { path: 'books', component: CatalogPage, title: 'Catalog - Bookstore' },
  { path: 'books/:isbn', component: BookPage, title: 'Book - Bookstore' },
];
