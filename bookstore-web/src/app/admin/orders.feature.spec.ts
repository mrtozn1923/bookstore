import type { AdminOrder } from '../models';
import { adminOrdersApiActions, adminOrdersPageActions } from './orders.actions';
import { adminOrdersFeature } from './orders.feature';

const orders: AdminOrder[] = [
  { id: 1041, customer: 'Alan', items: [], total: 640, status: 'new' },
  { id: 1040, customer: 'Ada', items: [], total: 490, status: 'shipped' },
];

describe('adminOrdersFeature', () => {
  // reducers and selectors are pure functions: no TestBed needed
  const { reducer, selectVisibleOrders, selectNewCount } = adminOrdersFeature;

  it('replaces a shipped order and clears its pending flag', () => {
    let state = reducer(undefined, adminOrdersApiActions.ordersLoaded({ orders }));
    state = reducer(state, adminOrdersPageActions.shipClicked({ id: 1041 }));
    expect(state.shippingIds).toEqual([1041]);
    state = reducer(state, adminOrdersApiActions.orderShipped({ order: { ...orders[0], status: 'shipped' } }));
    expect(state.shippingIds).toEqual([]);
    expect(state.orders.map((o) => o.status)).toEqual(['shipped', 'shipped']);
  });

  it('filters by status through the projector', () => {
    expect(selectVisibleOrders.projector(orders, 'new').map((o) => o.id)).toEqual([1041]);
    expect(selectNewCount.projector(orders)).toBe(1);
  });
});
