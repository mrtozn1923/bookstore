import { withDevtools } from '@angular-architects/ngrx-toolkit';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { signalStore } from '@ngrx/signals';
import { prependEntity, withEntities } from '@ngrx/signals/entities';
import { Events, on, withEventHandlers, withReducer } from '@ngrx/signals/events';
import { exhaustMap } from 'rxjs';
import { apiUrl } from '../api';
import { checkoutEvents, ordersApiEvents } from '../checkout/checkout-events';
import type { PlacedOrder } from '../models';
import { withRequestLog } from '../shared/with-request-log';
import { setError, setFulfilled, setPending, withRequestStatus } from '../shared/with-request-status';

// Orders placed in this browser session; the full order history is server state
export const OrdersStore = signalStore(
  { providedIn: 'root' },
  withEntities<PlacedOrder>(),
  withRequestStatus(),
  withRequestLog('orders'),
  // State transitions: event in, partial state out
  withReducer(
    on(checkoutEvents.orderSubmitted, () => setPending()),
    on(ordersApiEvents.orderPlaced, ({ payload }) => [prependEntity(payload), setFulfilled()]),
    on(ordersApiEvents.orderFailed, ({ payload }) => setError(payload)),
  ),
  // Side effects: the HTTP call; the event returned by the handler is dispatched
  withEventHandlers((store, events = inject(Events), http = inject(HttpClient)) => ({
    placeOrder$: events.on(checkoutEvents.orderSubmitted).pipe(
      // a second click while the order is being sent is ignored
      exhaustMap(({ payload }) =>
        http.post<{ id: number }>(`${apiUrl}/orders`, payload).pipe(
          mapResponse({
            next: ({ id }) => ordersApiEvents.orderPlaced({ id, ...payload }),
            error: (error: HttpErrorResponse) => ordersApiEvents.orderFailed(`HTTP ${error.status}`),
          }),
        ),
      ),
    ),
  })),
  withDevtools('orders'),
);
