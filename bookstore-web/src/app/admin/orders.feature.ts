import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import type { AdminOrder, OrderStatus } from '../models';
import { adminOrdersApiActions, adminOrdersPageActions } from './orders.actions';

interface AdminOrdersState {
  orders: AdminOrder[];
  statusFilter: OrderStatus | 'all';
  loading: boolean;
  shippingIds: number[];
  error: string | null;
}

const initialState: AdminOrdersState = { orders: [], statusFilter: 'all', loading: false, shippingIds: [], error: null };

// Feature = name + reducer + generated selectors (selectOrders, selectStatusFilter, ...)
export const adminOrdersFeature = createFeature({
  name: 'adminOrders',
  // explicit return types: the compiler rejects a misspelled or unknown state key
  reducer: createReducer(
    initialState,
    on(adminOrdersPageActions.opened, (state): AdminOrdersState => ({ ...state, loading: true, error: null })),
    on(adminOrdersPageActions.statusFilterChanged, (state, { status }): AdminOrdersState => ({ ...state, statusFilter: status })),
    on(adminOrdersPageActions.shipClicked, (state, { id }): AdminOrdersState => ({ ...state, shippingIds: [...state.shippingIds, id] })),
    on(adminOrdersApiActions.ordersLoaded, (state, { orders }): AdminOrdersState => ({ ...state, orders, loading: false })),
    on(adminOrdersApiActions.ordersLoadFailed, (state, { error }): AdminOrdersState => ({ ...state, loading: false, error })),
    on(adminOrdersApiActions.orderShipped, (state, { order }): AdminOrdersState => ({
      ...state,
      orders: state.orders.map((o) => (o.id === order.id ? order : o)),
      shippingIds: state.shippingIds.filter((id) => id !== order.id),
    })),
    on(adminOrdersApiActions.orderShipFailed, (state, { id, error }): AdminOrdersState => ({
      ...state,
      shippingIds: state.shippingIds.filter((x) => x !== id),
      error,
    })),
  ),
  // Derived data: memoized, recomputed only when an input selector returns a new value
  extraSelectors: ({ selectOrders, selectStatusFilter }) => ({
    selectVisibleOrders: createSelector(selectOrders, selectStatusFilter, (orders, status) =>
      status === 'all' ? orders : orders.filter((o) => o.status === status),
    ),
    selectNewCount: createSelector(selectOrders, (orders) => orders.filter((o) => o.status === 'new').length),
  }),
});
