import { Component, computed, inject } from '@angular/core';
import { CartStore } from '../shared/cart-store';
import { formatPrice } from '../shared/format';

@Component({
  selector: 'app-cart-page',
  template: `
    <h2>Cart</h2>
    @for (line of cart.lines(); track line.book.isbn) {
      <p>{{ line.quantity }} × {{ line.book.title }}</p>
    } @empty {
      <p>Your cart is empty.</p>
    }
    <p>Total: {{ total() }}</p>
  `,
})
export default class CartPage {
  protected readonly cart = inject(CartStore);
  protected readonly total = computed(() => formatPrice(this.cart.total()));
}
