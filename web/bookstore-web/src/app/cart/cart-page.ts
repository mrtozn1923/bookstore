import { CurrencyPipe } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Cart } from '../core/cart';
import { Session } from '../core/session';

interface PriceChange {
  isbn: string;
  cartPrice: number;
  currentPrice: number;
}

@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <h1>Cart</h1>
    @for (line of cart.lines(); track line.isbn) {
      <div class="line">
        <a [routerLink]="['/books', line.isbn]">{{ line.title }}</a>
        <input type="number" min="0" max="99" [value]="line.quantity"
               (change)="cart.setQuantity(line.isbn, $any($event.target).valueAsNumber)" />
        <span>{{ line.unitPrice * line.quantity | currency: line.currency }}</span>
      </div>
    } @empty {
      <p>Your cart is empty.</p>
    }

    @if (cart.count() > 0) {
      <p class="total">Total: {{ cart.total() | currency: cart.lines()[0].currency }}</p>
      @if (session.isSignedIn()) {
        <button (click)="placeOrder()" [disabled]="placing()">Place order</button>
      } @else {
        <button (click)="session.signIn('/cart')">Sign in to order</button>
      }
    }
    @if (problem()) {
      <p class="error">{{ problem() }}</p>
    }
  `
})
export class CartPage {
  protected readonly cart = inject(Cart);
  protected readonly session = inject(Session);
  protected readonly placing = signal(false);
  protected readonly problem = signal<string | undefined>(undefined);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  protected placeOrder(): void {
    this.placing.set(true);
    this.problem.set(undefined);
    const items = this.cart.lines().map(l => ({ isbn: l.isbn, quantity: l.quantity, unitPrice: l.unitPrice }));

    this.http.post('/api/orders', { items }, { observe: 'response' }).subscribe({
      next: response => {
        // 202 Accepted: the order exists, its outcome is not known yet. Location says where to watch it.
        const location = response.headers.get('Location') ?? '';
        this.cart.clear();
        void this.router.navigateByUrl(location.replace('/api/orders/', '/orders/'));
      },
      error: (error: HttpErrorResponse) => {
        this.placing.set(false);
        this.problem.set(this.explain(error));
      }
    });
  }

  private explain(error: HttpErrorResponse): string {
    switch (error.status) {
      case 409: {
        // Prices changed since the books were added: show the new prices, the customer decides again
        const changes = (error.error?.changes ?? []) as PriceChange[];
        this.cart.applyPrices(changes);
        return 'Some prices have changed. Check the new total and place the order again.';
      }
      case 429:
        return 'Too many orders in a short time. Wait a few seconds and try again.';
      case 0:
        // The request may or may not have reached the server: do not retry blindly, an order may already exist
        return 'The connection was lost. Check "My orders" before trying again.';
      default:
        return error.error?.title ?? 'The order could not be placed. Try again in a moment.';
    }
  }
}
