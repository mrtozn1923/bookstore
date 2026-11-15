import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type { components } from './bookstore-api';

export type BookDto = components['schemas']['BookDto'];
export type OrderDto = components['schemas']['OrderDto'];
export type CouponDto = components['schemas']['CouponDto'];
export type PlaceOrderRequest = components['schemas']['PlaceOrderRequest'];
export type PlaceBulkOrderRequest = components['schemas']['PlaceBulkOrderRequest'];

@Injectable({ providedIn: 'root' })
export class BookApi {
  private readonly http = inject(HttpClient);

  search(term: string): Observable<BookDto[]> {
    return this.http.get<BookDto[]>('/api/books', { params: { search: term } });
  }

  coupon(code: string): Observable<CouponDto> {
    return this.http.get<CouponDto>(`/api/coupons/${encodeURIComponent(code)}`);
  }

  placeOrder(request: PlaceOrderRequest): Observable<OrderDto> {
    return this.http.post<OrderDto>('/api/orders', request);
  }

  order(id: number): Observable<OrderDto> {
    return this.http.get<OrderDto>(`/api/orders/${id}`);
  }

  placeBulkOrder(request: PlaceBulkOrderRequest): Observable<OrderDto> {
    return this.http.post<OrderDto>('/api/institutional/orders', request);
  }

  invoice(id: number): Observable<Blob> {
    return this.http.get(`/api/orders/${id}/invoice`, { responseType: 'blob' });
  }
}
