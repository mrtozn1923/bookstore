import { httpResource } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { DeviceCapabilities } from '../core/device-capabilities';
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
      <ul class="orders">
        @for (order of orders.value().orders; track order) {
          <li>
            Order {{ order }}
            @if (device.supports.clipboard) {
              <button (click)="copy(order)">
                {{ copied() === order ? 'Copied' : 'Copy number' }}
              </button>
            }
          </li>
        }
      </ul>
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
  protected readonly device = inject(DeviceCapabilities);
  protected readonly copied = signal<number | null>(null);

  constructor() {
    // the user has seen the updates: clear the count and the app icon badge
    void inject(OrderBadge)
      .markRead()
      .catch(() => {});
  }

  async copy(order: number) {
    // customers paste the number into e-mails and support chats
    if (await this.device.copyText(String(order))) this.copied.set(order);
  }

  async enablePush() {
    console.log(`push: ${await this.push.enable()}`);
  }
}
