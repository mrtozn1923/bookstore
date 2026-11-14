import { Component, inject } from '@angular/core';
import { Store } from '@ngrx/store';
import type { OrderStatus } from '../models';
import { adminOrdersPageActions } from './orders.actions';
import { adminOrdersFeature } from './orders.feature';

@Component({
  selector: 'app-admin-orders-page',
  template: `
    <h1>Back office: orders ({{ newCount() }} new)</h1>
    <select class="status-filter" [value]="statusFilter()" (change)="filter($any($event.target).value)">
      <option value="all">All</option>
      <option value="new">New</option>
      <option value="shipped">Shipped</option>
    </select>
    @if (error(); as error) {
      <p class="admin-error">{{ error }}</p>
    }
    <ul class="admin-orders">
      @for (order of orders(); track order.id) {
        <li>
          #{{ order.id }} {{ order.customer }} - {{ order.total }} TRY - {{ order.status }}
          @if (order.status === 'new') {
            <button class="ship" [disabled]="shippingIds().includes(order.id)" (click)="ship(order.id)">Ship</button>
          }
        </li>
      }
    </ul>
  `,
})
export class AdminOrdersPage {
  readonly #store = inject(Store);
  // selectSignal: a selector read as a signal
  protected readonly orders = this.#store.selectSignal(adminOrdersFeature.selectVisibleOrders);
  protected readonly newCount = this.#store.selectSignal(adminOrdersFeature.selectNewCount);
  protected readonly statusFilter = this.#store.selectSignal(adminOrdersFeature.selectStatusFilter);
  protected readonly shippingIds = this.#store.selectSignal(adminOrdersFeature.selectShippingIds);
  protected readonly error = this.#store.selectSignal(adminOrdersFeature.selectError);

  constructor() {
    this.#store.dispatch(adminOrdersPageActions.opened());
  }

  protected filter(status: OrderStatus | 'all'): void {
    this.#store.dispatch(adminOrdersPageActions.statusFilterChanged({ status }));
  }

  protected ship(id: number): void {
    this.#store.dispatch(adminOrdersPageActions.shipClicked({ id }));
  }
}
