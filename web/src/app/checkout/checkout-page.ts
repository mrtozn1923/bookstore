import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CartStore } from '../cart/cart-store';
import { OrderApi } from '../orders/order-api';
import { CheckoutDetails, CheckoutForm } from './checkout-form';

@Component({
  selector: 'app-checkout-page',
  imports: [CheckoutForm],
  template: `
    <h1>Checkout</h1>
    @if (failed()) {
      <p role="alert">We could not place your order. Please try again.</p>
    }
    <app-checkout-form (placeOrder)="place($event)" />
  `,
})
export class CheckoutPage {
  private readonly orders = inject(OrderApi);
  private readonly cart = inject(CartStore);
  private readonly router = inject(Router);
  protected readonly failed = signal(false);

  async place(details: CheckoutDetails): Promise<void> {
    this.failed.set(false);
    try {
      const lines = this.cart.lines().map((line) => ({ isbn: line.book.isbn, quantity: line.quantity }));
      const order = await this.orders.place({ ...details, lines });
      this.cart.clear();
      await this.router.navigate(['/orders', order.id]);
    } catch {
      this.failed.set(true);
    }
  }
}
