import { createActionGroup, emptyProps, props } from '@ngrx/store';
import type { AdminOrder, OrderStatus } from '../models';

// Actions describe events from one source; the type becomes '[Admin Orders Page] Opened'
export const adminOrdersPageActions = createActionGroup({
  source: 'Admin Orders Page',
  events: {
    Opened: emptyProps(),
    'Status Filter Changed': props<{ status: OrderStatus | 'all' }>(),
    'Ship Clicked': props<{ id: number }>(),
  },
});

export const adminOrdersApiActions = createActionGroup({
  source: 'Admin Orders API',
  events: {
    'Orders Loaded': props<{ orders: AdminOrder[] }>(),
    'Orders Load Failed': props<{ error: string }>(),
    'Order Shipped': props<{ order: AdminOrder }>(),
    'Order Ship Failed': props<{ id: number; error: string }>(),
  },
});
