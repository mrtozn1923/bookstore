import { Component, input } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { environment } from '../../environments/environment';

interface TrackedOrder {
  id: number;
  books: string;
  total: number;
  status: string;
}

@Component({
  selector: 'app-order-tracking',
  template: `
    @if (order.error()) {
      <p>Order not found</p>
    } @else if (order.isLoading()) {
      <p>Loading order #{{ orderId() }}...</p>
    } @else if (order.value(); as o) {
      <h3>Order #{{ o.id }}</h3>
      <p>{{ o.books }} · {{ o.total }} TRY · {{ o.status }}</p>
    }
  `
})
export class OrderTrackingComponent {
  readonly orderId = input.required<number>();
  protected readonly order = httpResource<TrackedOrder>(() => `${environment.apiUrl}/orders/${this.orderId()}`);
}
