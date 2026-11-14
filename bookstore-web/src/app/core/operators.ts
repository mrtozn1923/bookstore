// Bookstore's custom RxJS operators (the same ones as src/11/operators.ts in the examples project).
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  Observable,
  of,
  pipe,
  retry,
  startWith,
  throwError,
  timer,
  type MonoTypeOperatorFunction,
  type OperatorFunction,
} from 'rxjs';

// a search term the user has stopped typing: wait, trim, skip repeats
export function settledTerm(dueTimeMs = 300): MonoTypeOperatorFunction<string> {
  return pipe(
    debounceTime(dueTimeMs),
    map((term) => term.trim()),
    distinctUntilChanged(),
  );
}

export interface BackoffOptions {
  count?: number;
  baseMs?: number;
  maxMs?: number;
  isRetryable?: (err: unknown) => boolean;
  random?: () => number;
  onRetry?: (err: unknown, retryCount: number, delayMs: number) => void;
}

// 500, 1000, 2000… ms ceilings (capped at maxMs), randomised between 50% and 100%
export function backoffDelay(retryCount: number, random = Math.random, baseMs = 500, maxMs = 8000) {
  const ceiling = Math.min(maxMs, baseMs * 2 ** (retryCount - 1));
  return Math.round(ceiling / 2 + (random() * ceiling) / 2);
}

// retry with exponential backoff and jitter, only for errors that may go away
export function retryWithBackoff<T>(options: BackoffOptions = {}): MonoTypeOperatorFunction<T> {
  const {
    count = 3,
    baseMs = 500,
    maxMs = 8000,
    isRetryable = () => true,
    random = Math.random,
    onRetry,
  } = options;
  return retry({
    count,
    delay: (err: unknown, retryCount) => {
      if (!isRetryable(err)) return throwError(() => err);
      const delayMs = backoffDelay(retryCount, random, baseMs, maxMs);
      onRetry?.(err, retryCount, delayMs);
      return timer(delayMs);
    },
  });
}

export type LoadingState<T> =
  { status: 'loading' } | { status: 'success'; data: T } | { status: 'error'; error: unknown };

// turn a request into states a template can show: loading, then success or error
export function loadingState<T>(): OperatorFunction<T, LoadingState<T>> {
  return (source) =>
    source.pipe(
      map((data): LoadingState<T> => ({ status: 'success', data })),
      startWith<LoadingState<T>>({ status: 'loading' }),
      catchError((error: unknown) => of<LoadingState<T>>({ status: 'error', error })),
    );
}

// from scratch: log every event of the stream, including when it is unsubscribed
export function debugTap<T>(
  label: string,
  log: (line: string) => void = console.log,
): MonoTypeOperatorFunction<T> {
  return (source) =>
    new Observable<T>((subscriber) => {
      log(`[${label}] subscribe`);
      let finished = false;
      const subscription = source.subscribe({
        next: (value) => {
          log(`[${label}] next ${JSON.stringify(value)}`);
          subscriber.next(value);
        },
        error: (err: unknown) => {
          finished = true;
          log(`[${label}] error ${err instanceof Error ? err.message : String(err)}`);
          subscriber.error(err);
        },
        complete: () => {
          finished = true;
          log(`[${label}] complete`);
          subscriber.complete();
        },
      });
      // teardown: runs on complete, error and unsubscribe; it must stop the source too
      return () => {
        if (!finished) log(`[${label}] unsubscribe`);
        subscription.unsubscribe();
      };
    });
}
