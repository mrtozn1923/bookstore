import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { CheckoutModel } from './checkout-form/checkout.model';

export interface PlacedOrder {
  id: number;
}

export interface ApiFieldError {
  field: string;
  message: string;
}

@Service()
export class OrderApiService {
  private readonly http = inject(HttpClient);

  place(order: CheckoutModel): Observable<PlacedOrder> {
    return this.http.post<PlacedOrder>(`${environment.apiUrl}/orders`, order);
  }
}
