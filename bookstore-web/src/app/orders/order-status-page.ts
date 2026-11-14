import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { OrderService } from './order-service';

@Component({
  selector: 'app-order-status-page',
  template: `
    <h1>Order 1042</h1>
    <p class="status">Status: {{ status()?.status ?? 'Loading...' }}</p>
  `,
})
export class OrderStatusPage {
  // toSignal subscribes now and unsubscribes when the page is destroyed
  protected readonly status = toSignal(inject(OrderService).status$(1042));
}
