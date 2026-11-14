import { CurrencyPipe, DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, effect, inject, input, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { Cart } from '../core/cart';
import { Order } from '../core/models';
import { OrderStatusHub } from '../core/order-status';

@Component({
  selector: 'app-order-page',
  imports: [CurrencyPipe, DatePipe],
  template: `
    @if (order.hasValue()) {
      @let o = order.value();
      <h1>Order {{ o.id.slice(0, 8) }}</h1>
      <p class="muted">Placed {{ o.placedAtUtc | date: 'medium' }}</p>
      @switch (o.status) {
        @case ('Confirmed') {
          <p class="status ok">Confirmed. A confirmation e-mail is on its way.</p>
        }
        @case ('Cancelled') {
          <p class="status error">Cancelled: {{ o.cancellationReason }}</p>
          <button (click)="putBack()">Put the books back in the cart</button>
        }
        @default {
          <!-- Pending, StockReserved, Paid: the saga is still running. Not "confirmed" yet. -->
          <p class="status pending">We received your order and are confirming stock and payment…</p>
        }
      }
      <ul>
        @for (item of o.items; track item.isbn) {
          <li>{{ item.quantity }} × {{ item.title }} — {{ item.lineTotal | currency: o.currency }}</li>
        }
      </ul>
      <p class="total">Total: {{ o.total | currency: o.currency }}</p>
      @if (hub.state() !== 'connected' && !final()) {
        <p class="muted">Live updates are paused; checking every 5 seconds.</p>
      }
    } @else if (order.error()) {
      <p class="error">This order cannot be shown right now.</p>
    } @else {
      <p>Loading…</p>
    }
  `
})
export class OrderPage {
  readonly id = input.required<string>();
  // Ordering is the source of truth; a push only tells us that it is worth asking again
  protected readonly order = httpResource<Order>(() => `/api/orders/${this.id()}`);
  protected readonly hub = inject(OrderStatusHub);
  protected readonly final = computed(() => {
    const status = this.order.hasValue() ? this.order.value().status : undefined;
    return status === 'Confirmed' || status === 'Cancelled';
  });
  private readonly cart = inject(Cart);
  private readonly router = inject(Router);

  constructor() {
    // A push for this order, or a reconnect after which pushes may have been missed: read the order again
    effect(() => {
      const change = this.hub.lastChange();
      this.hub.resyncs();
      if (!change || change.orderId === this.id()) {
        untracked(() => this.order.reload());
      }
    });

    // No live connection (Notifications down, a proxy that drops WebSockets): fall back to polling
    effect(onCleanup => {
      if (this.hub.state() === 'connected' || this.final()) {
        return;
      }
      const timer = setInterval(() => this.order.reload(), 5000);
      onCleanup(() => clearInterval(timer));
    });
  }

  protected putBack(): void {
    const o = this.order.value()!;
    for (const item of o.items) {
      this.cart.add({ isbn: item.isbn, title: item.title, unitPrice: item.unitPrice, currency: o.currency }, item.quantity);
    }
    void this.router.navigateByUrl('/cart');
  }
}
