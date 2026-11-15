import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { AuthService } from './auth.service';
import { HomeComponent } from './home/home.component';
import { BookListComponent } from './catalog/book-list/book-list.component';
import { BookDetailComponent } from './book-detail/book-detail.component';
import { BookSummaryComponent } from './book-summary/book-summary.component';
import { BookReviewsComponent } from './book-reviews/book-reviews.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { OrderListComponent } from './order-list/order-list.component';
import { LoginComponent } from './login/login.component';
import { AddressEditComponent } from './address-edit/address-edit.component';
import { authGuard, authMatchGuard, roleGuard } from './auth.guard';
import { unsavedChangesGuard } from './unsaved-changes.guard';
import { bookResolver } from './book.resolver';
import { SignUpComponent } from './sign-up/sign-up.component';
import { CartComponent } from './cart/cart.component';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, title: 'Home' },
  { path: 'books', component: BookListComponent, title: 'Books' },
  {
    path: 'books/:isbn',
    component: BookDetailComponent,
    resolve: { book: bookResolver },
    children: [
      { path: '', redirectTo: 'summary', pathMatch: 'full' },
      { path: 'summary', component: BookSummaryComponent },
      { path: 'reviews', component: BookReviewsComponent }
    ]
  },
  {
    path: 'about',
    loadComponent: () => import('./about/about.component').then(m => m.AboutComponent),
    title: 'About'
  },
  { path: 'cart', component: CartComponent, title: 'Cart' },
  { path: 'login', component: LoginComponent },
  {
    path: 'checkout',
    loadComponent: () => import('./checkout-form/checkout-form.component').then(m => m.CheckoutFormComponent),
    title: 'Checkout'
  },
  { path: 'sign-up', component: SignUpComponent, title: 'Sign up' },
  { path: 'orders', component: OrderListComponent, canActivate: [authGuard], title: 'My orders' },
  { path: 'account/address', component: AddressEditComponent, canDeactivate: [unsavedChangesGuard] },
  {
    path: 'my-account',
    redirectTo: ({ queryParams }) => {
      const auth = inject(AuthService);
      if (!auth.isLoggedIn()) {
        return '/login';
      }
      return queryParams['tab'] === 'address' ? '/account/address' : '/orders';
    }
  },
  {
    path: 'admin',
    loadChildren: () => import('./admin/admin.routes').then(m => m.ADMIN_ROUTES),
    canMatch: [authMatchGuard],
    canActivate: [roleGuard('Admin')]
  },
  { path: '**', component: NotFoundComponent }
];
