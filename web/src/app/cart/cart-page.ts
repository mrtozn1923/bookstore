import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { BookApi, type CouponDto } from '../api/book-api.service';
import { CartStore } from './cart-store';

@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <h1>Cart</h1>
    @if (cart.lines().length === 0) {
      <p>Your cart is empty. <a routerLink="/">Browse the catalog</a></p>
    } @else {
      <table class="cart-lines">
        <thead>
          <tr><th scope="col">Book</th><th scope="col">Quantity</th><th scope="col">Price</th></tr>
        </thead>
        <tbody>
          @for (line of cart.lines(); track line.book.id) {
            <tr>
              <td>{{ line.book.title }}</td>
              <td>{{ line.quantity }}</td>
              <td>{{ line.book.price * line.quantity | currency: 'TRY' : 'symbol-narrow' }}</td>
            </tr>
          }
        </tbody>
      </table>
      <p class="subtotal">Subtotal: {{ cart.subtotal() | currency: 'TRY' : 'symbol-narrow' }}</p>

      <form class="coupon" (submit)="applyCoupon(code.value); $event.preventDefault()">
        <label for="coupon">Coupon code</label>
        <input id="coupon" #code autocomplete="off" />
        <button type="submit">Apply</button>
      </form>
      @if (coupon(); as applied) {
        <p class="discount">Discount ({{ applied.code }}): -{{ discount() | currency: 'TRY' : 'symbol-narrow' }}</p>
      }
      <p class="total">Total: {{ total() | currency: 'TRY' : 'symbol-narrow' }}</p>

      <form class="checkout" (submit)="placeOrder(city.value); $event.preventDefault()">
        <label for="city">Shipping city</label>
        <input id="city" #city autocomplete="address-level2" />
        <button type="submit" [disabled]="placing()">Place order</button>
      </form>
    }
    @if (error(); as message) {
      <p class="error" role="alert">{{ message }}</p>
    }
  `,
})
export class CartPage {
  private readonly api = inject(BookApi);
  private readonly router = inject(Router);
  protected readonly cart = inject(CartStore);
  protected readonly coupon = signal<CouponDto | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly placing = signal(false);
  protected readonly discount = computed(() =>
    Math.round(this.cart.subtotal() * (this.coupon()?.percentOff ?? 0)) / 100,
  );
  protected readonly total = computed(() => this.cart.subtotal() - this.discount());

  protected applyCoupon(code: string): void {
    this.error.set(null);
    this.api.coupon(code.trim()).subscribe({
      next: (coupon) => this.coupon.set(coupon),
      error: () => {
        this.coupon.set(null);
        this.error.set(`Coupon ${code} is not valid.`);
      },
    });
  }

  protected placeOrder(city: string): void {
    this.error.set(null);
    this.placing.set(true);
    const items = this.cart.lines().map((line) => ({ bookId: line.book.id, quantity: line.quantity }));
    this.api.placeOrder({ items, shippingCity: city, couponCode: this.coupon()?.code ?? null }).subscribe({
      next: (order) => {
        this.cart.clear();
        this.router.navigate(['/orders', order.id]);
      },
      error: (response: HttpErrorResponse) => {
        this.placing.set(false);
        this.error.set(
          response.status === 401
            ? 'Please sign in to place an order.'
            : (response.error?.title ?? 'The order could not be placed.'),
        );
      },
    });
  }
}
