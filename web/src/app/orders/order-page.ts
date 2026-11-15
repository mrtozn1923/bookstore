import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { BookApi, type OrderDto } from '../api/book-api.service';
import { statusLabel } from '../catalog/labels';

const cancellationWindowMs = 30 * 60 * 1000;

@Component({
  selector: 'app-order-page',
  imports: [CurrencyPipe, DatePipe],
  template: `
    @if (order(); as current) {
      <h1>{{ statusLabel(current) }}</h1>
      <dl class="order-summary">
        <dt>Reference</dt>
        <dd>{{ current.reference }}</dd>
        <dt>Total</dt>
        <dd>{{ current.total | currency: 'TRY' : 'symbol-narrow' }}</dd>
        <dt>Tracking number</dt>
        <dd>{{ current.trackingNumber ?? 'Pending' }}</dd>
      </dl>
      <p class="cancel-hint">You can cancel this order until {{ cancelUntil(current) | date: 'HH:mm' }}.</p>
    } @else {
      <p>Loading order…</p>
    }
  `,
})
export class OrderPage {
  private readonly api = inject(BookApi);
  readonly id = input.required<string>();
  protected readonly order = signal<OrderDto | null>(null);
  protected readonly statusLabel = statusLabel;

  constructor() {
    effect(() => {
      this.api.order(Number(this.id())).subscribe((order) => this.order.set(order));
    });
  }

  protected cancelUntil(order: OrderDto): Date {
    return new Date(new Date(order.createdAt).getTime() + cancellationWindowMs);
  }
}
