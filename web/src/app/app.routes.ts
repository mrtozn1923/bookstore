import { Routes } from '@angular/router';
import { CartPage } from './cart/cart-page';
import { CatalogPage } from './catalog/catalog-page';
import { BulkOrderPage } from './institutional/bulk-order-page';
import { OrderPage } from './orders/order-page';

export const routes: Routes = [
  { path: '', component: CatalogPage, title: 'Catalog | Bookstore' },
  { path: 'cart', component: CartPage, title: 'Cart | Bookstore' },
  { path: 'orders/:id', component: OrderPage, title: 'Order | Bookstore' },
  { path: 'institutional/bulk-order', component: BulkOrderPage, title: 'Bulk order | Bookstore' },
];
