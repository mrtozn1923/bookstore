import { Routes } from '@angular/router';
import { authGuard } from './auth/auth-guard';
import { orderTitle } from './orders/order-title';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./catalog/catalog-page').then((m) => m.CatalogPage) },
  { path: 'cart', loadComponent: () => import('./cart/cart-page').then((m) => m.CartPage) },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadComponent: () => import('./checkout/checkout-page').then((m) => m.CheckoutPage),
  },
  {
    path: 'orders/:id',
    canActivate: [authGuard],
    title: orderTitle,
    loadComponent: () => import('./orders/order-detail').then((m) => m.OrderDetail),
  },
  { path: 'sign-in', loadComponent: () => import('./auth/sign-in-page').then((m) => m.SignInPage) },
];
