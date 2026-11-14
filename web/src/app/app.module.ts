import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

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
    LifecycleDemoComponent
  ],
  imports: [
    BrowserModule,
    CatalogModule,
    FormsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
