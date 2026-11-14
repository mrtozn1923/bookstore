import { HttpErrorResponse, httpResource } from '@angular/common/http';
import { Component, computed, input } from '@angular/core';
import { PricePipe } from '../shared/price-pipe';
import { Order } from './order';

@Component({
  selector: 'app-order-detail',
  imports: [PricePipe],
  template: `
    @switch (order.status()) {
      @case ('error') {
        <p role="alert">{{ errorMessage() }}</p>
      }
      @case ('resolved') {
        @if (order.value(); as order) {
          <h1>Order {{ order.reference }}</h1>
          <p class="status">Status: {{ order.status }}</p>
          <ul aria-label="Order lines">
            @for (line of order.lines; track line.isbn) {
              <li>{{ line.quantity }} × {{ line.title }}</li>
            }
          </ul>
          <p class="total">Total: {{ order.total | price }}</p>
        }
      }
      @default {
        <p>Loading order…</p>
      }
    }
  `,
})
export class OrderDetail {
  readonly id = input.required<string>();
  protected readonly order = httpResource<Order>(() => `/api/orders/${this.id()}`);
  protected readonly errorMessage = computed(() => {
    const error = this.order.error();
    return error instanceof HttpErrorResponse && [403, 404].includes(error.status)
      ? 'This order could not be found.'
      : 'Could not load the order.';
  });
}
