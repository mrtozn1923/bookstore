import { HttpErrorResponse } from '@angular/common/http';
import { computed } from '@angular/core';
import { signalStoreFeature, withComputed, withState, type SignalStoreFeatureType } from '@ngrx/signals';

export type RequestStatus = 'idle' | 'pending' | 'fulfilled' | 'error';
export type RequestStatusState = { requestStatus: RequestStatus; requestError: string | null };

// Reusable feature: every store that talks to the API gets the same status fields
export function withRequestStatus() {
  return signalStoreFeature(
    withState<RequestStatusState>({ requestStatus: 'idle', requestError: null }),
    withComputed(({ requestStatus }) => ({
      isPending: computed(() => requestStatus() === 'pending'),
    })),
  );
}

// Updaters are plain functions, so they combine with other updaters in a single patchState call
export function setPending(): RequestStatusState {
  return { requestStatus: 'pending', requestError: null };
}

export function setFulfilled(): RequestStatusState {
  return { requestStatus: 'fulfilled', requestError: null };
}

export function setError(error: unknown): RequestStatusState {
  const message = error instanceof HttpErrorResponse ? `HTTP ${error.status}` : String(error);
  return { requestStatus: 'error', requestError: message };
}

// NgRx 22: the members this feature adds, reusable as the input type of another feature
export type RequestStatusFeature = SignalStoreFeatureType<typeof withRequestStatus>;
