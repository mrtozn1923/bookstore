import { Component, inject } from '@angular/core';
import { OrdersStore } from './orders-store';

@Component({
  selector: 'app-orders-page',
  template: `
    <h1>Orders</h1>
    <ul class="orders">
      @for (order of orders.entities(); track order.id) {
        <li>
          #{{ order.id }}:
          @for (item of order.items; track item.isbn) {
            {{ item.title }} x {{ item.quantity }}
          }
          - {{ order.total }} TRY ({{ order.shipping }})
        </li>
      } @empty {
        <li>No orders in this session.</li>
      }
    </ul>
  `,
})
export class OrdersPage {
  protected readonly orders = inject(OrdersStore);
}
