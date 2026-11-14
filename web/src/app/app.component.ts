import { Component } from '@angular/core';
import { ScrollTopComponent } from './scroll-top/scroll-top.component';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SaleCountdownComponent } from './sale-countdown/sale-countdown.component';
import { CartBadgeComponent } from './cart-badge/cart-badge.component';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css'],
    imports: [SaleCountdownComponent, RouterLink, RouterLinkActive, RouterOutlet, ScrollTopComponent, CartBadgeComponent]
})
export class AppComponent {
  title = 'bookstore-web';
  price = 450;
  showSale = false;
  count = 0;
  visible = true;
  reservations = ['Dune', 'Emma'];
}
