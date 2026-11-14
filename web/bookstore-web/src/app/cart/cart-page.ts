import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrderView } from '../core/api-models';
import { CartStore } from './cart-store';

@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './cart-page.html',
})
export class CartPage {
  protected readonly store = inject(CartStore);
  protected readonly placedOrder = signal<OrderView | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly couponError = signal('');

  protected async changeQuantity(isbn: string, quantity: number): Promise<void> {
    if (quantity >= 1 && quantity <= 99) {
      await this.store.setQuantity(isbn, quantity);
    }
  }

  protected async applyCoupon(event: Event, code: string): Promise<void> {
    event.preventDefault();
    this.couponError.set('');
    try {
      await this.store.applyCoupon(code);
    } catch (error) {
      const problem = error instanceof HttpErrorResponse ? error.error : null;
      this.couponError.set(problem?.detail ?? 'The coupon could not be applied.');
    }
  }

  protected async checkout(event: Event, name: string, email: string): Promise<void> {
    event.preventDefault();
    this.errorMessage.set('');
    try {
      this.placedOrder.set(await this.store.placeOrder(name, email));
    } catch (error) {
      // The API answers with ProblemDetails: 400 validation errors or 409 when stock is not enough.
      const problem = error instanceof HttpErrorResponse ? error.error : null;
      this.errorMessage.set(problem?.detail ?? problem?.title ?? 'The order could not be placed.');
    }
  }
}
