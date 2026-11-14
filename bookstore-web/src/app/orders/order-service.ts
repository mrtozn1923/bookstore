import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { exhaustMap, takeWhile, timer } from 'rxjs';
import { apiUrl } from '../api';
import type { CartItem } from '../cart/cart-store';

export interface OrderStatus {
  id: number;
  status: 'Paid' | 'Shipped' | 'Delivered';
}

export interface PlacedOrder {
  id: number;
  status: string;
}

@Service()
export class OrderService {
  readonly #http = inject(HttpClient);

  placeOrder(items: readonly CartItem[]) {
    return this.#http.post<PlacedOrder>(`${apiUrl}/orders`, {
      items: items.map((i) => ({ isbn: i.book.isbn, quantity: i.quantity })),
    });
  }

  // polls every 5 seconds until the order is delivered (see the time-based operators post)
  status$(orderId: number) {
    return timer(0, 5000).pipe(
      exhaustMap(() => this.#http.get<OrderStatus>(`${apiUrl}/orders/${orderId}/status`)),
      takeWhile((order) => order.status !== 'Delivered', true),
    );
  }
}
