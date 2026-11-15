import { SelectionModel } from '@angular/cdk/collections';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpClient, httpResource } from '@angular/common/http';
import { Component, inject, input, linkedSignal, numberAttribute } from '@angular/core';
import { MatBottomSheet } from '@angular/material/bottom-sheet';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatOption } from '@angular/material/core';
import { MatDialog } from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatSelect } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort, MatSortHeader, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { OrderRow, PagedResult } from '../../core/api-models';
import { CancelOrderData, CancelOrderDialog, CancelOrderResult } from './cancel-order-dialog';
import { OrderAction, OrderActionsSheet } from './order-actions-sheet';

export const pageSize = 25;
const statuses = ['Placed', 'PaymentPending', 'Paid', 'Shipped', 'Delivered', 'Cancelled'];
const cancellable = ['Placed', 'PaymentPending', 'Paid'];

@Component({
  selector: 'app-orders-page',
  imports: [
    MatTableModule, MatSort, MatSortHeader, MatPaginator, MatCheckbox, MatProgressBar, MatFormField, MatLabel, MatSelect,
    MatOption, MatButton, MatIconButton, MatIcon, CurrencyPipe, DatePipe,
  ],
  templateUrl: './orders-page.html',
  styleUrl: './orders-page.scss',
})
export class OrdersPage {
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly bottomSheet = inject(MatBottomSheet);
  private readonly snackBar = inject(MatSnackBar);
  private readonly money = new CurrencyPipe('en-US');

  // The URL is the state: ?status=Cancelled&sort=total&dir=desc&page=3 (withComponentInputBinding).
  // A missing query param arrives as undefined, not as the input's default, hence the transforms.
  readonly status = input('', { transform: (value: string | undefined) => value ?? '' });
  readonly sort = input('placedAtUtc', { transform: (value: string | undefined) => value ?? 'placedAtUtc' });
  readonly dir = input<'asc' | 'desc', string | undefined>('desc', { transform: value => (value === 'asc' ? 'asc' : 'desc') });
  readonly page = input(1, { transform: (value: unknown) => Math.max(1, numberAttribute(value, 1)) });

  protected readonly statuses = statuses;
  protected readonly pageSize = pageSize;
  protected readonly columns = ['select', 'id', 'placedAtUtc', 'customer', 'status', 'itemCount', 'total', 'actions'];

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

  // Same order, same row element after a reload: focus and selection stay where they were
  protected readonly trackById = (_: number, order: OrderRow) => order.id;

  protected canCancel(order: OrderRow): boolean {
    return cancellable.includes(order.status);
  }

  protected cancelOne(order: OrderRow): void {
    const total = this.money.transform(order.total, order.currency, 'symbol-narrow');
    this.cancelOrders([order.id], `${order.customerName} · ${total}`);
  }

  protected openActions(): void {
    const ids = [...this.selection.selected];
    this.bottomSheet
      .open<OrderActionsSheet, { count: number }, OrderAction>(OrderActionsSheet, { data: { count: ids.length } })
      .afterDismissed()
      .subscribe(action => {
        if (action === 'cancel') {
          this.cancelOrders(ids, `${ids.length} selected orders`);
        } else if (action === 'clear') {
          this.selection.clear();
        }
      });
  }

  private cancelOrders(orderIds: number[], summary: string): void {
    const dialogRef = this.dialog.open<CancelOrderDialog, CancelOrderData, CancelOrderResult>(CancelOrderDialog, {
      data: { orderIds, summary },
      width: '440px',
      // The default only closes on browser back/forward; close on every route change
      closeOnNavigation: 'url-change',
    });
    // afterClosed() emits once (undefined when dismissed with Esc, the backdrop or "Keep order") and completes
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        void this.submitCancel(orderIds, result.reason);
      }
    });
  }

  private async submitCancel(orderIds: number[], reason: string): Promise<void> {
    const results = await Promise.allSettled(
      orderIds.map(id => firstValueFrom(this.http.post(`/api/admin/orders/${id}/cancel`, { reason }))));
    const failed = results.filter(r => r.status === 'rejected').length;
    this.selection.deselect(...orderIds);
    this.orders.reload();
    const done = orderIds.length - failed;
    const message = failed === 0
      ? `${orderIds.length === 1 ? `Order #${orderIds[0]}` : `${done} orders`} cancelled`
      : `${done} cancelled, ${failed} could not be cancelled`;
    this.snackBar.open(message, 'OK', { duration: 5000 });
  }

  private navigate(queryParams: Record<string, string | number | null>): void {
    void this.router.navigate([], { queryParams, queryParamsHandling: 'merge' });
  }
}
