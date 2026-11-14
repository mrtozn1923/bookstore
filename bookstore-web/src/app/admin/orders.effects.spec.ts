import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, of, throwError } from 'rxjs';
import type { AdminOrder } from '../models';
import { adminOrdersApiActions, adminOrdersPageActions } from './orders.actions';
import { loadOrders } from './orders.effects';

describe('loadOrders effect', () => {
  // a functional effect is a function: its dependencies are passed as arguments
  it('maps the response to Orders Loaded', async () => {
    const orders = [{ id: 1041 } as AdminOrder];
    const http = { get: () => of(orders) } as unknown as HttpClient;
    const action = await firstValueFrom(loadOrders(of(adminOrdersPageActions.opened()), http));
    expect(action).toEqual(adminOrdersApiActions.ordersLoaded({ orders }));
  });

  it('maps an HTTP error to Orders Load Failed', async () => {
    const http = { get: () => throwError(() => new HttpErrorResponse({ status: 500 })) } as unknown as HttpClient;
    const action = await firstValueFrom(loadOrders(of(adminOrdersPageActions.opened()), http));
    expect(action).toEqual(adminOrdersApiActions.ordersLoadFailed({ error: 'HTTP 500' }));
  });
});
