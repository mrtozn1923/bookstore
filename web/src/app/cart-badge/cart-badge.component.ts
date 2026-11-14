import { Component } from '@angular/core';
import { CartService } from '../cart.service';

@Component({
  selector: 'app-cart-badge',
  template: `
    <span>Books in cart: {{ count$ | async }}</span>
    <div *ngIf="items$ | async as items">
      <p *ngFor="let item of items">{{ item.title }}</p>
    </div>
  `
})
export class CartBadgeComponent {
  count$ = this.cartService.count$;
  items$ = this.cartService.items$;

  constructor(private cartService: CartService) { }
}
