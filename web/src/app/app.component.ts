import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ScrollTopComponent } from './scroll-top/scroll-top.component';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SaleCountdownComponent } from './sale-countdown/sale-countdown.component';
import { CartBadgeComponent } from './cart-badge/cart-badge.component';
import { ToastHostComponent } from './toast-host/toast-host.component';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [CurrencyPipe, SaleCountdownComponent, RouterLink, RouterLinkActive, RouterOutlet, ScrollTopComponent, CartBadgeComponent, ToastHostComponent]
})
export class AppComponent {
  title = 'bookstore-web';
  freeShippingLimit = 500;
  price = 450;
  showSale = false;
  count = 0;
  visible = true;
  reservations = ['Dune', 'Emma'];
}
