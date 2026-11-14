import { signalStore, withState } from '@ngrx/signals';
import { on, withReducer } from '@ngrx/signals/events';
import { ordersApiEvents } from '../checkout/checkout-events';

// Reacts to the same events as the cart and the orders store, without knowing either of them
export const NotificationsStore = signalStore(
  { providedIn: 'root' },
  withState({ message: null as string | null }),
  withReducer(
    on(ordersApiEvents.orderPlaced, ({ payload }) => ({ message: `Order #${payload.id} placed` })),
    on(ordersApiEvents.orderFailed, ({ payload }) => ({ message: `Order failed (${payload})` })),
  ),
);
