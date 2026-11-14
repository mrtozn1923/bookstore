import { Routes } from '@angular/router';
import { BookPage } from './catalog/book-page';
import { CatalogPage } from './catalog/catalog-page';
import { CartPage } from './cart/cart-page';
import { MyOrdersPage } from './orders/my-orders-page';
import { OrderPage } from './orders/order-page';

export const routes: Routes = [
  { path: '', component: CatalogPage, title: 'Bookstore' },
  { path: 'books/:isbn', component: BookPage, title: 'Book' },
  { path: 'cart', component: CartPage, title: 'Cart' },
  { path: 'orders', component: MyOrdersPage, title: 'My orders' },
  { path: 'orders/:id', component: OrderPage, title: 'Order' },
  { path: '**', redirectTo: '' }
];
