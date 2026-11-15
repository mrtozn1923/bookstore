import { Routes } from '@angular/router';
import { BookDetailPage } from './catalog/book-detail-page';
import { CartPage } from './cart/cart-page';
import { CatalogPage } from './catalog/catalog-page';
import { StoreLayout } from './store-layout';

export const routes: Routes = [
  {
    path: '',
    component: StoreLayout,
    children: [
      { path: '', component: CatalogPage, title: 'Bookstore' },
      { path: 'books/:isbn', component: BookDetailPage, title: 'Book · Bookstore' },
      { path: 'cart', component: CartPage, title: 'Cart · Bookstore' },
    ],
  },
  // Back office: its own chunk, the only part of the app that uses Angular Material
  { path: 'admin', loadChildren: () => import('./admin/admin.routes').then(m => m.adminRoutes) },
  { path: '**', redirectTo: '' },
];
