import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CartService } from '../cart.service';
import { AsyncPipe } from '@angular/common';

@Component({
    selector: 'app-cart-badge',
    template: `
    @let count = count$ | async;
    @let items = items$ | async;

    <span>Books in cart: {{ count }}</span>
    @for (item of items; track $index) {
      <p>{{ item.title }}</p>
    }
  `,
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [AsyncPipe]
})
export class CartBadgeComponent {
  count$ = this.cartService.count$;
  items$ = this.cartService.items$;

  constructor(private cartService: CartService) { }
}
