import { Routes } from '@angular/router';
import { CartPage } from './cart/cart-page';
import { CatalogPage } from './catalog/catalog-page';

export const routes: Routes = [
  { path: '', component: CatalogPage },
  { path: 'cart', component: CartPage },
];
