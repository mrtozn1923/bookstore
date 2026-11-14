import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, retry, throwError, timer } from 'rxjs';

export interface CreateOrder { institutionUnitId: number; bookId: number; quantity: number; totalAmount: number; }
export interface OrderDto { id: number; totalAmount: number; }

@Injectable({ providedIn: 'root' })
export class OrderApi {
  private readonly http = inject(HttpClient);

  create(request: CreateOrder, idempotencyKey: string): Observable<OrderDto> {
    return this.http
      .post<OrderDto>('/api/orders', request, {
        headers: { 'Idempotency-Key': `"${idempotencyKey}"` }
      })
      .pipe(
        retry({
          count: 3,
          // retry with the same key only on network errors, 5xx and 409
          delay: (error: HttpErrorResponse, attempt: number) =>
            error.status === 0 || error.status === 409 || error.status >= 500
              ? timer(attempt * 1000)
              : throwError(() => error)
        })
      );
  }
}

export class OrderForm {
  private readonly api = inject(OrderApi);
  // generated once when the form opens; stays the same however many times the user clicks "Save"
  private idempotencyKey = crypto.randomUUID();
  saving = false;

  save(request: CreateOrder): void {
    this.saving = true;
    this.api.create(request, this.idempotencyKey).subscribe({
      next: () => {
        this.saving = false;
        this.idempotencyKey = crypto.randomUUID(); // new order, new key
      },
      error: () => (this.saving = false) // the key stays: a retry by the user is the same request
    });
  }
}
