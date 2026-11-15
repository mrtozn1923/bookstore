import { Routes } from '@angular/router';
import { BookDetailPage } from './catalog/book-detail-page';
import { AccessibilityPage } from './accessibility/accessibility-page';
import { CartPage } from './cart/cart-page';
import { CatalogPage } from './catalog/catalog-page';

export const routes: Routes = [
  { path: '', component: CatalogPage, title: 'Bookstore' },
  { path: 'books/:isbn', component: BookDetailPage, title: 'Book · Bookstore' },
  { path: 'cart', component: CartPage, title: 'Cart · Bookstore' },
  { path: 'accessibility', component: AccessibilityPage, title: 'Accessibility statement · Bookstore' },
  { path: '**', redirectTo: '' },
];
