import { Component } from '@angular/core';
import { OrderFormComponent } from './orders/order-form';

@Component({
  selector: 'app-root',
  imports: [OrderFormComponent],
  template: `
    <h1>Bookstore - Institutional Ordering</h1>
    <app-order-form />
  `,
})
export class App {}
