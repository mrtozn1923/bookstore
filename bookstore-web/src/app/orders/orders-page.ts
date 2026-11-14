import { httpResource } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { OrderBadge } from '../core/order-badge';

interface MyOrders {
  customer: string;
  orders: number[];
}

@Component({
  selector: 'app-orders-page',
  template: `
    <h1>My orders</h1>
    @if (orders.hasValue()) {
      <p class="orders">Orders: {{ orders.value().orders.join(', ') }}</p>
    } @else if (orders.error()) {
      <p class="orders">Your orders need a connection. Please try again when you are online.</p>
    }
  `,
})
export class OrdersPage {
  // never cached by the service worker: not in any dataGroup
  protected readonly orders = httpResource<MyOrders>(() => '/api/me/orders');

  constructor() {
    // the user has seen the updates: clear the count and the app icon badge
    void inject(OrderBadge)
      .markRead()
      .catch(() => {});
  }
}
