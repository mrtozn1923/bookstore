import { Component, OnInit } from '@angular/core';
import { Customer, CustomerService } from '../customer.service';
import { NgIf, NgFor } from '@angular/common';

@Component({
    selector: 'app-customer-list',
    templateUrl: './customer-list.component.html',
    styleUrls: ['./customer-list.component.css'],
    standalone: true,
    imports: [NgIf, NgFor]
})
export class CustomerListComponent implements OnInit {
  customers: Customer[] = [];
  errorMessage = '';

  constructor(private customerService: CustomerService) { }

  ngOnInit(): void {
    this.customerService.getCustomers().subscribe({
      next: customers => this.customers = customers,
      error: () => this.errorMessage = 'The list cannot be loaded right now, please try again shortly.'
    });
  }

  addCustomer(): void {
    this.customerService.createCustomer({ name: 'Linus Torvalds', email: 'linus@example.com' })
      .subscribe(created => this.customers.push(created));
  }
}
