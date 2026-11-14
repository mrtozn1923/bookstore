import { Routes } from '@angular/router';
import { CartPage } from './cart/cart-page';
import { BookDetailsPage } from './catalog/book-details-page';
import { CatalogPage } from './catalog/catalog-page';
import { SharePanel } from './catalog/share-panel';
import { CheckoutReview } from './checkout/checkout-review';
import { CheckoutShipping } from './checkout/checkout-shipping';
import { CheckoutStore } from './checkout/checkout-store';

export const routes: Routes = [
  { path: '', component: CatalogPage },
  {
    path: 'books/:isbn',
    component: BookDetailsPage,
    children: [{ path: 'share', component: SharePanel }],
  },
  { path: 'cart', component: CartPage },
  {
    path: 'checkout',
    // one CheckoutStore for all checkout steps; gone when the user leaves checkout
    providers: [CheckoutStore],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'shipping' },
      { path: 'shipping', component: CheckoutShipping },
      { path: 'review', component: CheckoutReview },
    ],
  },
];
