import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { exhaustMap, mergeMap } from 'rxjs';
import { apiUrl } from '../api';
import type { AdminOrder } from '../models';
import { adminOrdersApiActions, adminOrdersPageActions } from './orders.actions';

// Functional effects: no class, dependencies as default parameters
export const loadOrders = createEffect(
  (actions$ = inject(Actions), http = inject(HttpClient)) => {
    return actions$.pipe(
      ofType(adminOrdersPageActions.opened),
      exhaustMap(() =>
        http.get<AdminOrder[]>(`${apiUrl}/orders`).pipe(
          mapResponse({
            next: (orders) => adminOrdersApiActions.ordersLoaded({ orders }),
            error: (error: HttpErrorResponse) => adminOrdersApiActions.ordersLoadFailed({ error: `HTTP ${error.status}` }),
          }),
        ),
      ),
    );
  },
  { functional: true },
);

export const shipOrder = createEffect(
  (actions$ = inject(Actions), http = inject(HttpClient)) => {
    return actions$.pipe(
      ofType(adminOrdersPageActions.shipClicked),
      // different orders can be shipped in parallel
      mergeMap(({ id }) =>
        http.post<AdminOrder>(`${apiUrl}/orders/${id}/ship`, {}).pipe(
          mapResponse({
            next: (order) => adminOrdersApiActions.orderShipped({ order }),
            error: (error: HttpErrorResponse) =>
              adminOrdersApiActions.orderShipFailed({ id, error: `HTTP ${error.status}` }),
          }),
        ),
      ),
    );
  },
  { functional: true },
);
