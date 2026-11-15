import { SelectionModel } from '@angular/cdk/collections';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, input, linkedSignal, numberAttribute } from '@angular/core';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatOption } from '@angular/material/core';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatSelect } from '@angular/material/select';
import { MatSort, MatSortHeader, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { OrderRow, PagedResult } from '../../core/api-models';

export const pageSize = 25;
const statuses = ['Placed', 'PaymentPending', 'Paid', 'Shipped', 'Delivered', 'Cancelled'];

@Component({
  selector: 'app-orders-page',
  imports: [
    MatTableModule, MatSort, MatSortHeader, MatPaginator, MatCheckbox, MatProgressBar, MatFormField, MatLabel, MatSelect,
    MatOption, CurrencyPipe, DatePipe,
  ],
  templateUrl: './orders-page.html',
  styleUrl: './orders-page.scss',
})
export class OrdersPage {
  private readonly router = inject(Router);

  // The URL is the state: ?status=Cancelled&sort=total&dir=desc&page=3 (withComponentInputBinding).
  // A missing query param arrives as undefined, not as the input's default, hence the transforms.
  readonly status = input('', { transform: (value: string | undefined) => value ?? '' });
  readonly sort = input('placedAtUtc', { transform: (value: string | undefined) => value ?? 'placedAtUtc' });
  readonly dir = input<'asc' | 'desc', string | undefined>('desc', { transform: value => (value === 'asc' ? 'asc' : 'desc') });
  readonly page = input(1, { transform: (value: unknown) => Math.max(1, numberAttribute(value, 1)) });

  protected readonly statuses = statuses;
  protected readonly pageSize = pageSize;
  protected readonly columns = ['select', 'id', 'placedAtUtc', 'customer', 'status', 'itemCount', 'total'];

  protected readonly orders = httpResource<PagedResult<OrderRow>>(() => ({
    url: '/api/admin/orders',
    params: { status: this.status(), sort: this.sort(), direction: this.dir(), page: this.page(), pageSize },
  }));

  // Keep the previous page on screen while the next one loads instead of an empty table
  protected readonly rows = linkedSignal<PagedResult<OrderRow> | undefined, OrderRow[]>({
    source: () => this.orders.value(),
    computation: (result, previous) => result?.items ?? previous?.value ?? [],
  });
  protected readonly total = linkedSignal<PagedResult<OrderRow> | undefined, number>({
    source: () => this.orders.value(),
    computation: (result, previous) => result?.total ?? previous?.value ?? 0,
  });

  // Selected order ids survive paging and sorting; rows are new objects on every response
  protected readonly selection = new SelectionModel<number>(true);

  // A method, not computed(): SelectionModel is not a signal, a computed() would keep its first answer
  protected allOnPageSelected(): boolean {
    const ids = this.rows().map(row => row.id);
    return ids.length > 0 && ids.every(id => this.selection.isSelected(id));
  }

  protected sortChanged(sort: Sort): void {
    this.navigate({ sort: sort.direction ? sort.active : null, dir: sort.direction || null, page: null });
  }

  protected pageChanged(event: PageEvent): void {
    this.navigate({ page: event.pageIndex + 1 });
  }

  protected statusChanged(status: string): void {
    // A new filter starts again from the first page
    this.navigate({ status: status || null, page: null });
  }

  protected toggleAllOnPage(): void {
    const ids = this.rows().map(row => row.id);
    if (this.allOnPageSelected()) {
      this.selection.deselect(...ids);
    } else {
      this.selection.select(...ids);
    }
  }

  private navigate(queryParams: Record<string, string | number | null>): void {
    void this.router.navigate([], { queryParams, queryParamsHandling: 'merge' });
  }
}
