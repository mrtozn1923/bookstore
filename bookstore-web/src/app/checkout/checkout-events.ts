import { type } from '@ngrx/signals';
import { eventGroup } from '@ngrx/signals/events';
import type { OrderRequest, PlacedOrder } from '../models';

// What happened, not what to do: the checkout page does not know who reacts
export const checkoutEvents = eventGroup({
  source: 'Checkout Page',
  events: {
    orderSubmitted: type<OrderRequest>(),
  },
});

export const ordersApiEvents = eventGroup({
  source: 'Orders API',
  events: {
    orderPlaced: type<PlacedOrder>(),
    orderFailed: type<string>(),
  },
});
