import { Routes } from '@angular/router';
import { CatalogPage } from './catalog/catalog-page';

export const routes: Routes = [
  { path: '', component: CatalogPage, title: 'Catalog · Bookstore' },
  { path: 'books/:isbn', loadComponent: () => import('./catalog/book-detail-page'), title: 'Book · Bookstore' },
  { path: 'cart', loadComponent: () => import('./cart/cart-page'), title: 'Cart · Bookstore' },
  { path: 'orders', loadComponent: () => import('./orders/orders-page'), title: 'My orders · Bookstore' },
  { path: 'admin', loadComponent: () => import('./admin/admin-page'), title: 'Admin · Bookstore' },
];
