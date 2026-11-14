import { CurrencyPipe, DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, effect, inject, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrderSummary } from '../core/models';
import { OrderStatusHub } from '../core/order-status';

@Component({
  selector: 'app-my-orders-page',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  template: `
    <h1>My orders</h1>
    @if (orders.hasValue()) {
      <ul>
        @for (o of orders.value(); track o.id) {
          <li>
            <a [routerLink]="['/orders', o.id]">{{ o.placedAtUtc | date: 'short' }}</a>
            {{ o.itemCount }} book(s), {{ o.total | currency: o.currency }} — <span class="status">{{ o.status }}</span>
          </li>
        } @empty {
          <li>No orders yet.</li>
        }
      </ul>
    } @else if (orders.error()) {
      <p class="error">Your orders cannot be shown right now.</p>
    }
  `
})
export class MyOrdersPage {
  protected readonly orders = httpResource<OrderSummary[]>(() => '/api/orders');
  private readonly hub = inject(OrderStatusHub);

  constructor() {
    // Any status change of any of the user's orders, or a reconnect: refresh the list
    effect(() => {
      this.hub.lastChange();
      this.hub.resyncs();
      untracked(() => this.orders.reload());
    });
  }
}
