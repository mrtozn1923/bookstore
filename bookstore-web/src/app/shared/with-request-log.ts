import { signalStoreFeature, type, watchState, withHooks } from '@ngrx/signals';
import type { RequestStatusFeature } from './with-request-status';

// Requires withRequestStatus() earlier in the same store; the compiler checks it.
// The unused generic avoids a known TypeScript issue when several features with input share a store.
export function withRequestLog<_>(name: string) {
  return signalStoreFeature(
    type<RequestStatusFeature>(),
    withHooks({
      onInit(store) {
        let previous = store.requestStatus();
        // watchState is synchronous: it sees every patchState call, not only the last one in a tick
        watchState(store, ({ requestStatus, requestError }) => {
          if (requestStatus === previous) return;
          console.log(`[${name}] ${previous} -> ${requestStatus}${requestError ? ` (${requestError})` : ''}`);
          previous = requestStatus;
        });
      },
    }),
  );
}
