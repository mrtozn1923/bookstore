import { Component } from '@angular/core';
import { CartService } from '../cart.service';
import { AsyncPipe } from '@angular/common';

@Component({
    selector: 'app-cart-badge',
    template: `
    <span>Books in cart: {{ count$ | async }}</span>
    @if (items$ | async; as items) {
      <div>
        @for (item of items; track $index) {
          <p>{{ item.title }}</p>
        }
      </div>
    }
    `,
    standalone: true,
    imports: [AsyncPipe]
})
export class CartBadgeComponent {
  count$ = this.cartService.count$;
  items$ = this.cartService.items$;

  constructor(private cartService: CartService) { }
}
