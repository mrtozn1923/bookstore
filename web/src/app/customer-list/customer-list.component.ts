import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { rxResource } from '@angular/core/rxjs-interop';
import { Customer, CustomerService } from '../customer.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-customer-list',
  templateUrl: './customer-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./customer-list.component.css']
})
export class CustomerListComponent {
  private http = inject(HttpClient);
  private customerService = inject(CustomerService);
  sort = signal<'asc' | 'desc'>('asc');

  customersResource = rxResource({
    params: () => ({ sort: this.sort() }),
    stream: ({ params }) =>
      this.http.get<Customer[]>(`${environment.apiUrl}/customers`, { params: { _sort: 'name', _order: params.sort } })
  });

  addCustomer(): void {
    this.customerService.createCustomer({ name: 'Linus Torvalds', email: 'linus@example.com' })
      .subscribe(() => this.customersResource.reload());
  }
}
