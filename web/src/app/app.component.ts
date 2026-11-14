import { Component } from '@angular/core';
import { ScrollTopComponent } from './scroll-top/scroll-top.component';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SaleCountdownComponent } from './sale-countdown/sale-countdown.component';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css'],
    standalone: true,
    imports: [SaleCountdownComponent, RouterLink, RouterLinkActive, RouterOutlet, ScrollTopComponent]
})
export class AppComponent {
  title = 'bookstore-web';
  price = 450;
  showSale = false;
  count = 0;
  visible = true;
  reservations = ['Dune', 'Emma'];
}
