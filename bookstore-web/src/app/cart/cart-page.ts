import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartStore } from './cart-store';

@Component({
  selector: 'app-cart-page',
  imports: [RouterLink],
  template: `
    <h1>Cart</h1>
    <ul class="cart-items">
      @for (item of cart.items(); track item.isbn) {
        <li>
          {{ item.title }} x {{ item.quantity }} = {{ item.price * item.quantity }} TRY
          <button (click)="cart.setQuantity(item.isbn, item.quantity - 1)">-</button>
          <button (click)="cart.setQuantity(item.isbn, item.quantity + 1)">+</button>
          <button (click)="cart.remove(item.isbn)">Remove</button>
        </li>
      } @empty {
        <li>Your cart is empty.</li>
      }
    </ul>
    <p>
      <input class="coupon-code" placeholder="Coupon" #code />
      <button class="apply" (click)="cart.applyCoupon(code.value)">Apply</button>
    </p>
    @if (cart.requestError(); as error) {
      <p class="coupon-error">Coupon not applied ({{ error }})</p>
    }
    @if (cart.coupon(); as coupon) {
      <p class="discount">{{ coupon.code }}: -{{ cart.discount() }} TRY</p>
    }
    <p class="total">Total: {{ cart.payable() }} TRY</p>
    @if (cart.count() > 0) {
      <a class="checkout-link" routerLink="/checkout">Checkout</a>
    }
  `,
})
export class CartPage {
  protected readonly cart = inject(CartStore);
}
