import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Order, PlaceOrderRequest } from './order';

@Injectable({ providedIn: 'root' })
export class OrderApi {
  private readonly http = inject(HttpClient);

  place(request: PlaceOrderRequest): Promise<Order> {
    return firstValueFrom(this.http.post<Order>('/api/orders', request));
  }
}
