import { Routes } from '@angular/router';
import { BookDetailPage } from './catalog/book-detail-page';
import { CatalogPage } from './catalog/catalog-page';
import { StoreLayout } from './store-layout';

export const routes: Routes = [
  {
    path: '',
    component: StoreLayout,
    children: [
      { path: '', component: CatalogPage, title: 'Bookstore' },
      { path: 'books/:isbn', component: BookDetailPage, title: 'Book · Bookstore' },
      // Lazy: the snackbar brings Material code that the catalog does not need
      {
        path: 'reading-list',
        loadComponent: () => import('./reading-list/reading-list-page').then(m => m.ReadingListPage),
        title: 'My reading list · Bookstore',
      },
      { path: 'cart', loadComponent: () => import('./cart/cart-page').then(m => m.CartPage), title: 'Cart · Bookstore' },
    ],
  },
  // Back office: its own chunk, the only part of the app that uses Angular Material
  { path: 'admin', loadChildren: () => import('./admin/admin.routes').then(m => m.adminRoutes) },
  { path: '**', redirectTo: '' },
];
