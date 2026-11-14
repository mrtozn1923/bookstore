import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartStore } from './cart-store';

@Component({
  selector: 'app-cart-page',
  imports: [RouterLink],
  template: `
    <h1>Cart</h1>
    <ul>
      @for (item of cart.items(); track item.book.isbn) {
        <li>{{ item.quantity }} x {{ item.book.title }}</li>
      } @empty {
        <li>Your cart is empty.</li>
      }
    </ul>
    <a routerLink="/">Back to catalog</a>
  `,
})
export class CartPage {
  protected readonly cart = inject(CartStore);
}
