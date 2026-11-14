import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { patchState, signalStore, withMethods } from '@ngrx/signals';
import { setError, setFulfilled, setPending, withRequestStatus } from './with-request-status';

// A feature is tested through a minimal store that uses it
const TestStore = signalStore(
  { providedIn: 'root' },
  withRequestStatus(),
  withMethods((store) => ({
    start: () => patchState(store, setPending()),
    succeed: () => patchState(store, setFulfilled()),
    fail: (error: unknown) => patchState(store, setError(error)),
  })),
);

describe('withRequestStatus', () => {
  it('starts idle and tracks a request', () => {
    const store = TestBed.inject(TestStore);
    expect(store.requestStatus()).toBe('idle');
    store.start();
    expect(store.isPending()).toBe(true);
    store.succeed();
    expect(store.requestStatus()).toBe('fulfilled');
  });

  it('turns an HTTP error into a short message', () => {
    expect(setError(new HttpErrorResponse({ status: 503 }))).toEqual({ requestStatus: 'error', requestError: 'HTTP 503' });
  });
});
