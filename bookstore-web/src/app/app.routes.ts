import { Routes } from '@angular/router';
import { CartPage } from './cart/cart-page';
import { BookPage } from './catalog/book-page';
import { CatalogPage } from './catalog/catalog-page';
import { OrderStatusPage } from './orders/order-status-page';
import { SearchPage } from './search/search-page';

export const routes: Routes = [
  { path: '', component: CatalogPage },
  { path: 'books/:isbn', component: BookPage },
  { path: 'search', component: SearchPage },
  { path: 'cart', component: CartPage },
  { path: 'orders/1042', component: OrderStatusPage },
];
