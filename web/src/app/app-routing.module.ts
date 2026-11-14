import { NgModule } from '@angular/core';
import { PreloadAllModules, RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { BookListComponent } from './catalog/book-list/book-list.component';
import { BookDetailComponent } from './book-detail/book-detail.component';
import { BookSummaryComponent } from './book-summary/book-summary.component';
import { BookReviewsComponent } from './book-reviews/book-reviews.component';
import { AboutComponent } from './about/about.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { OrderListComponent } from './order-list/order-list.component';
import { LoginComponent } from './login/login.component';
import { AddressEditComponent } from './address-edit/address-edit.component';
import { AuthGuard } from './auth.guard';
import { UnsavedChangesGuard } from './unsaved-changes.guard';
import { BookResolver } from './book.resolver';
import { CheckoutComponent } from './checkout/checkout.component';
import { SignUpComponent } from './sign-up/sign-up.component';

const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, title: 'Home' },
  { path: 'books', component: BookListComponent, title: 'Books' },
  {
    path: 'books/:isbn',
    component: BookDetailComponent,
    resolve: { book: BookResolver },
    children: [
      { path: '', redirectTo: 'summary', pathMatch: 'full' },
      { path: 'summary', component: BookSummaryComponent },
      { path: 'reviews', component: BookReviewsComponent }
    ]
  },
  { path: 'about', component: AboutComponent, title: 'About' },
  { path: 'login', component: LoginComponent },
  { path: 'checkout', component: CheckoutComponent, title: 'Checkout' },
  { path: 'sign-up', component: SignUpComponent, title: 'Sign up' },
  { path: 'orders', component: OrderListComponent, canActivate: [AuthGuard], title: 'My orders' },
  { path: 'account/address', component: AddressEditComponent, canDeactivate: [UnsavedChangesGuard] },
  {
    path: 'admin',
    loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule),
    canLoad: [AuthGuard],
    canActivate: [AuthGuard]
  },
  { path: '**', component: NotFoundComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { preloadingStrategy: PreloadAllModules })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
