import { httpResource } from '@angular/common/http';
import { Component } from '@angular/core';

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
}
