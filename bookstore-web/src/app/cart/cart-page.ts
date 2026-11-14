import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { errorMessage } from '../core/problem-details';
import { OrderService } from '../orders/order-service';
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
    <button type="button" (click)="placeOrder()" [disabled]="placing() || cart.count() === 0">
      {{ placing() ? 'Placing order...' : 'Place order' }}
    </button>
    @if (message(); as text) {
      <p class="order-message">{{ text }}</p>
    }
    <a routerLink="/">Back to catalog</a>
  `,
})
export class CartPage {
  protected readonly cart = inject(CartStore);
  readonly #orders = inject(OrderService);
  protected readonly placing = signal(false);
  protected readonly message = signal<string | null>(null);

  protected placeOrder(): void {
    this.placing.set(true);
    this.message.set(null);
    this.#orders
      .placeOrder(this.cart.items())
      // the button comes back whatever happens: success or error
      .pipe(finalize(() => this.placing.set(false)))
      // eslint-disable-next-line rxjs-x/no-ignored-subscription -- one POST: HttpClient completes after the response
      .subscribe({
        next: (order) => this.message.set(`Order ${order.id} placed.`),
        // component level: this page knows what a 409 means for the user
        error: (err: unknown) => this.message.set(errorMessage(err)),
      });
  }
}
