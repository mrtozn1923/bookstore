import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { tapResponse } from '@ngrx/operators';
import { patchState, signalStore, withFeature, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { debounceTime, distinctUntilChanged, EMPTY, map, pipe, switchMap } from 'rxjs';
import { apiUrl } from '../api';
import type { Book, BookPage } from '../models';
import { withKeyboardSelection } from '../shared/with-keyboard-selection';
import { withRequestLog } from '../shared/with-request-log';
import { setError, setFulfilled, setPending, withRequestStatus } from '../shared/with-request-status';

// Local UI state with server calls: provided by the search box component, one instance per box
export const SearchStore = signalStore(
  withState({ query: '', results: [] as Book[] }),
  withRequestStatus(),
  withRequestLog('search'),
  withFeature(({ results }) => withKeyboardSelection(results)),
  withProps(() => ({ _http: inject(HttpClient) })),
  withMethods((store) => ({
    setQuery(query: string): void {
      patchState(store, { query });
    },
    // RxJS where it pays off: wait for a pause in typing, cancel the request that is no longer needed
    _search: rxMethod<string>(
      pipe(
        map((query) => query.trim()),
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((query) => {
          if (query.length < 2) {
            patchState(store, { results: [], requestStatus: 'idle', requestError: null });
            return EMPTY;
          }
          patchState(store, setPending());
          return store._http.get<BookPage>(`${apiUrl}/books`, { params: { q: query } }).pipe(
            // handles the error inside the inner stream, so the outer stream keeps running
            tapResponse({
              next: (page) => patchState(store, { results: page.items }, setFulfilled()),
              error: (error) => patchState(store, { results: [] }, setError(error)),
            }),
          );
        }),
      ),
    ),
  })),
  withHooks({
    // Called with a signal: the stream gets every new query; cleaned up with the store's injector
    onInit: (store) => store._search(store.query),
  }),
);
