import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Session } from './session';

// The BFF rejects state-changing /api calls without this header (post 10). A form or an image on
// another site cannot add a custom header; HttpClient can.
export const csrfHeader: HttpInterceptorFn = (request, next) =>
  request.method === 'GET' || request.method === 'HEAD'
    ? next(request)
    : next(request.clone({ setHeaders: { 'X-CSRF': '1' } }));

// A 401 from /api means the session (or the access token behind it) is gone: sign in again and come back here.
// /bff/user is excluded: there a 401 only means "not signed in yet".
export const sessionExpired: HttpInterceptorFn = (request, next) => {
  const session = inject(Session);
  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && request.url.startsWith('/api/')) {
        session.signIn(location.pathname + location.search);
      }
      return throwError(() => error);
    })
  );
};
