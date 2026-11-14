import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { rxResource } from '@angular/core/rxjs-interop';
import { Customer, CustomerService } from '../customer.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-customer-list',
  templateUrl: './customer-list.component.html',
  styleUrls: ['./customer-list.component.css']
})
export class CustomerListComponent {
  private http = inject(HttpClient);
  private customerService = inject(CustomerService);
  sort = signal<'asc' | 'desc'>('asc');

  customersResource = rxResource({
    request: () => ({ sort: this.sort() }),
    loader: ({ request }) =>
      this.http.get<Customer[]>(`${environment.apiUrl}/customers`, { params: { _sort: 'name', _order: request.sort } })
  });

  addCustomer(): void {
    this.customerService.createCustomer({ name: 'Linus Torvalds', email: 'linus@example.com' })
      .subscribe(() => this.customersResource.reload());
  }
}
