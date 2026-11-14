import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./catalog/catalog-page').then((m) => m.CatalogPage) },
  { path: 'cart', loadComponent: () => import('./cart/cart-page').then((m) => m.CartPage) },
];
