import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';
import { environment } from '../environments/environment';

export interface Customer {
  id: number;
  name: string;
  email: string;
}

export interface CustomerOrder {
  id: number;
  customerId: number;
  total: number;
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private apiUrl = `${environment.apiUrl}/customers`;

  constructor(private http: HttpClient) { }

  getCustomers(): Observable<Customer[]> {
    return this.http.get<Customer[]>(this.apiUrl).pipe(
      retry(2),
      catchError((error: HttpErrorResponse) => {
        console.error('Could not load customers:', error.status, error.message);
        return throwError(() => error);
      })
    );
  }

  createCustomer(customer: Omit<Customer, 'id'>): Observable<Customer> {
    return this.http.post<Customer>(this.apiUrl, customer);
  }

  updateCustomer(customer: Customer): Observable<Customer> {
    return this.http.put<Customer>(`${this.apiUrl}/${customer.id}`, customer);
  }

  deleteCustomer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getOrdersByCustomer(customerId: number): Observable<CustomerOrder[]> {
    const params = new HttpParams()
      .set('customerId', String(customerId))
      .set('_limit', '5');

    const headers = new HttpHeaders({ 'Accept-Language': 'en' });

    return this.http.get<CustomerOrder[]>(`${environment.apiUrl}/orders`, { params, headers });
  }
}
