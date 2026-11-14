import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { apiUrl } from '../api';
import type { CartItem } from '../cart/cart-store';

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
}
