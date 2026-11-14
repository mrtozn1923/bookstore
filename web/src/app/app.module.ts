import { LOCALE_ID, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { AuthInterceptor } from './auth.interceptor';
import { ErrorInterceptor } from './error.interceptor';
import { registerLocaleData } from '@angular/common';
import localeTr from '@angular/common/locales/tr';

import { AppComponent } from './app.component';
import { BookCardComponent } from './book-card/book-card.component';
import { CatalogModule } from './catalog/catalog.module';
import { BookDetailComponent } from './book-detail/book-detail.component';
import { OrderListComponent } from './order-list/order-list.component';
import { HighlightDirective } from './highlight.directive';
import { QuantityPickerComponent } from './quantity-picker/quantity-picker.component';
import { CardComponent } from './card/card.component';
import { OrderTrackingComponent } from './order-tracking/order-tracking.component';
import { PriceTagComponent } from './price-tag/price-tag.component';
import { SearchBoxComponent } from './search-box/search-box.component';
import { SaleCountdownComponent } from './sale-countdown/sale-countdown.component';
import { LifecycleDemoComponent } from './lifecycle-demo/lifecycle-demo.component';
import { CustomerCardComponent } from './customer-card/customer-card.component';
import { OrderSummaryComponent } from './order-summary/order-summary.component';
import { environment } from '../environments/environment';
import { APP_CONFIG } from './app-config';
import { LoggerService, SilentLoggerService } from './logger.service';
import { CartBadgeComponent } from './cart-badge/cart-badge.component';
import { ReservationTimerComponent } from './reservation-timer/reservation-timer.component';
import { BookSearchComponent } from './book-search/book-search.component';
import { CartNotifierComponent } from './cart-notifier/cart-notifier.component';
import { CustomerListComponent } from './customer-list/customer-list.component';
import { AppRoutingModule } from './app-routing.module';
import { HomeComponent } from './home/home.component';
import { AboutComponent } from './about/about.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { BookSummaryComponent } from './book-summary/book-summary.component';
import { BookReviewsComponent } from './book-reviews/book-reviews.component';
import { LoginComponent } from './login/login.component';
import { AddressEditComponent } from './address-edit/address-edit.component';

registerLocaleData(localeTr);

@NgModule({
  declarations: [
    AppComponent,
    BookCardComponent,
    BookDetailComponent,
    OrderListComponent,
    HighlightDirective,
    QuantityPickerComponent,
    CardComponent,
    OrderTrackingComponent,
    PriceTagComponent,
    SearchBoxComponent,
    SaleCountdownComponent,
    LifecycleDemoComponent,
    CustomerCardComponent,
    OrderSummaryComponent,
    CartBadgeComponent,
    ReservationTimerComponent,
    BookSearchComponent,
    CartNotifierComponent,
    CustomerListComponent,
    HomeComponent,
    AboutComponent,
    NotFoundComponent,
    BookSummaryComponent,
    BookReviewsComponent,
    LoginComponent,
    AddressEditComponent
  ],
  imports: [
    BrowserModule,
    CatalogModule,
    FormsModule,
    HttpClientModule,
    AppRoutingModule
  ],
  providers: [
    { provide: LOCALE_ID, useValue: 'tr' },
    {
      provide: LoggerService,
      useFactory: () => environment.production ? new SilentLoggerService() : new LoggerService()
    },
    { provide: APP_CONFIG, useValue: { appName: 'Bookstore', pageSize: 20 } },
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
