import { Component, inject } from '@angular/core';
import { CartStore } from '../cart/cart-store';
import { CheckoutStore } from './checkout-store';

@Component({
  selector: 'app-checkout-review',
  template: `
    <h1>Checkout: review</h1>
    <p class="review">
      Books {{ cart.totalPrice() }} TRY + {{ checkout.shipping() }} shipping
      {{ checkout.shippingPrice() }} TRY = {{ checkout.payable() }} TRY
    </p>
  `,
})
export class CheckoutReview {
  protected readonly cart = inject(CartStore);
  protected readonly checkout = inject(CheckoutStore);
}
