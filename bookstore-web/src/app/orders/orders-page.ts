import { httpResource } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { OrderBadge } from '../core/order-badge';
import { PushNotifications } from '../core/push-notifications';

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
    <!-- ask only here, where the value is obvious, and only after a click -->
    @if (push.supported && !push.subscribed() && push.permission() !== 'denied') {
      <button class="notify" (click)="enablePush()">Notify me when my orders ship</button>
    }
    @if (push.subscribed()) {
      <p class="push">
        Shipping notifications are on. <button (click)="push.disable()">Turn off</button>
      </p>
    }
    @if (push.permission() === 'denied') {
      <p class="push">Notifications are blocked for Bookstore in your browser settings.</p>
    }
  `,
})
export class OrdersPage {
  // never cached by the service worker: not in any dataGroup
  protected readonly orders = httpResource<MyOrders>(() => '/api/me/orders');
  protected readonly push = inject(PushNotifications);

  constructor() {
    // the user has seen the updates: clear the count and the app icon badge
    void inject(OrderBadge)
      .markRead()
      .catch(() => {});
  }

  async enablePush() {
    console.log(`push: ${await this.push.enable()}`);
  }
}
