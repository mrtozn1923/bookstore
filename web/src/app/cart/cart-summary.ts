import { Component, inject } from '@angular/core';
import { PricePipe } from '../shared/price-pipe';
import { CartStore } from './cart-store';
import { QuantityInput } from './quantity-input';

@Component({
  selector: 'app-cart-summary',
  imports: [PricePipe, QuantityInput],
  template: `
    <section aria-label="Cart summary">
      @if (cart.lines().length === 0) {
        <p class="empty">Your cart is empty.</p>
      } @else {
        <ul>
          @for (line of cart.lines(); track line.book.isbn) {
            <li>
              <span class="title">{{ line.book.title }}</span>
              <app-quantity-input
                [quantity]="line.quantity"
                (quantityChange)="cart.setQuantity(line.book.isbn, $event)"
                [max]="line.book.stock"
              />
              <span class="line-total">{{ line.book.price * line.quantity | price }}</span>
            </li>
          }
        </ul>
        <p class="total">Total: {{ cart.total() | price }}</p>
        <ng-content select="[cartActions]" />
      }
    </section>
  `,
})
export class CartSummary {
  protected readonly cart = inject(CartStore);
}
