import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrderView } from '../core/api-models';
import { CartStore } from './cart-store';

// The fields of an RFC 9457 problem response that the page shows.
interface ProblemDetails {
  title?: string;
  detail?: string;
}

@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './cart-page.html',
})
export class CartPage {
  protected readonly store = inject(CartStore);
  protected readonly placedOrder = signal<OrderView | null>(null);
  protected readonly errorMessage = signal('');

  protected async changeQuantity(isbn: string, quantity: number): Promise<void> {
    if (quantity >= 1 && quantity <= 99) {
      await this.store.setQuantity(isbn, quantity);
    }
  }

  protected async checkout(event: Event, name: string, email: string): Promise<void> {
    event.preventDefault();
    this.errorMessage.set('');
    try {
      this.placedOrder.set(await this.store.placeOrder(name, email));
    } catch (error) {
      // The API answers with ProblemDetails: 400 validation errors or 409 when stock is not enough.
      const problem =
        error instanceof HttpErrorResponse ? (error.error as ProblemDetails | null) : null;
      this.errorMessage.set(problem?.detail ?? problem?.title ?? 'The order could not be placed.');
    }
  }

  protected async clearCart(): Promise<void> {
    // One request at a time: every response replaces the whole cart state.
    for (const line of this.store.cart()?.items ?? []) {
      await this.store.remove(line.isbn);
    }
  }
}
