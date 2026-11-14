import { Component, inject } from '@angular/core';
import { CartStore } from './cart-store';

@Component({
  selector: 'app-cart-page',
  template: `
    <h1>Cart</h1>
    <ul class="cart-items">
      @for (item of cart.items(); track item.isbn) {
        <li>{{ item.title }} x {{ item.quantity }}</li>
      } @empty {
        <li>Your cart is empty.</li>
      }
    </ul>
  `,
})
export class CartPage {
  protected readonly cart = inject(CartStore);
}
