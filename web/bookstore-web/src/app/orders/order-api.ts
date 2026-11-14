import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

// UML enumeration OrderStatus -> string union (API enum'ları string olarak döner)
export type OrderStatus =
  | 'Draft'
  | 'PendingApprover'
  | 'PendingPurchasingManager'
  | 'Approved'
  | 'Rejected'
  | 'Cancelled';

export type ShipmentPreference = 'SingleShipment' | 'PartialAllowed';

export interface CreateOrder {
  placedById: number;
  budgetCategoryCode: string;
  bookId: number;
  quantity: number;
  deliveryDate: string;
  note: string | null;
  submit: boolean;
  shipmentPreference: ShipmentPreference;
  minFirstShipment: number | null;
}

export interface OrderResult {
  id: number;
  status: OrderStatus;
  total: number;
  shipmentPreference: ShipmentPreference;
  minFirstShipment: number | null;
}

@Injectable({ providedIn: 'root' })
export class OrderApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/orders';

  // UC-01: POST /orders
  create(body: CreateOrder): Observable<OrderResult> {
    return this.http.post<OrderResult>(this.baseUrl, body);
  }

  // UC-01: POST /orders/{id}/submit
  submit(id: number): Observable<OrderResult> {
    return this.http.post<OrderResult>(`${this.baseUrl}/${id}/submit`, {});
  }
}
