import { HttpErrorResponse, HttpResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, retry, tap, throwError, timer } from 'rxjs';
import { ServerStatus } from './server-status';

// network errors (status 0) and 5xx may succeed on the next attempt; 4xx will not
const isRetryable = (err: unknown) =>
  err instanceof HttpErrorResponse && (err.status === 0 || err.status >= 500);

// 500, 1000, 2000 ms ceilings, randomised between 50% and 100%
const backoff = (retryCount: number) => {
  const ceiling = 500 * 2 ** (retryCount - 1);
  return ceiling / 2 + (Math.random() * ceiling) / 2;
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const serverStatus = inject(ServerStatus);
  // only GET is safe to repeat; a POST could create a second order
  const retries = req.method === 'GET' ? 3 : 0;

  return next(req).pipe(
    retry({
      count: retries,
      delay: (err, retryCount) => {
        if (!isRetryable(err)) return throwError(() => err);
        console.warn(`[http] ${req.method} ${req.url} failed (${err.status}), retry ${retryCount}`);
        return timer(backoff(retryCount));
      },
    }),
    tap((event) => {
      if (event instanceof HttpResponse) serverStatus.report(false);
    }),
    catchError((err: unknown) => {
      // central part: record it and show the global banner; the caller still gets the error
      if (isRetryable(err)) serverStatus.report(true);
      console.error(`[http] ${req.method} ${req.url} failed:`, err instanceof HttpErrorResponse ? err.status : err);
      return throwError(() => err);
    }),
  );
};
