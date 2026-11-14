import { Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Events, injectDispatch } from '@ngrx/signals/events';
import { CartStore } from '../cart/cart-store';
import { OrdersStore } from '../orders/orders-store';
import { checkoutEvents, ordersApiEvents } from './checkout-events';
import { CheckoutStore } from './checkout-store';

@Component({
  selector: 'app-checkout-review',
  template: `
    <h1>Checkout: review</h1>
    <p class="review">
      Books {{ cart.payable() }} TRY + {{ checkout.shipping() }} shipping
      {{ checkout.shippingPrice() }} TRY = {{ checkout.payable() }} TRY
    </p>
    <button class="place-order" [disabled]="orders.isPending()" (click)="placeOrder()">Place order</button>
    @if (orders.requestError(); as error) {
      <p class="order-error">Could not place the order ({{ error }})</p>
    }
  `,
})
export class CheckoutReview {
  protected readonly cart = inject(CartStore);
  protected readonly checkout = inject(CheckoutStore);
  protected readonly orders = inject(OrdersStore);
  readonly #dispatch = injectDispatch(checkoutEvents);

  constructor() {
    const router = inject(Router);
    // A component can listen to events too; the subscription ends with the component
    inject(Events)
      .on(ordersApiEvents.orderPlaced)
      .pipe(takeUntilDestroyed())
      .subscribe(() => void router.navigate(['/orders']));
  }

  protected placeOrder(): void {
    this.#dispatch.orderSubmitted({
      items: this.cart.items(),
      shipping: this.checkout.shipping(),
      total: this.checkout.payable(),
    });
  }
}
