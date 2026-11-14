import { Component } from '@angular/core';
import { formatLongDate } from '../shared/dates';

interface OrderSummary {
  id: string;
  placedAt: string;
  status: 'Placed' | 'Shipped' | 'Delivered';
}

@Component({
  selector: 'app-orders-page',
  template: `
    <h2>My orders</h2>
    @for (order of orders; track order.id) {
      <p>{{ order.id }} · {{ placed(order) }} · {{ order.status }}</p>
    }
  `,
})
export default class OrdersPage {
  protected readonly orders: readonly OrderSummary[] = [
    { id: 'BS-1042', placedAt: '2026-11-12T14:30:00Z', status: 'Shipped' },
    { id: 'BS-0977', placedAt: '2026-10-03T08:15:00Z', status: 'Delivered' },
  ];

  protected placed(order: OrderSummary): string {
    return formatLongDate(order.placedAt);
  }
}
