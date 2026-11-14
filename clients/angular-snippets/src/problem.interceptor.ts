import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from './notification.service';
import { isProblem, problemCode } from './problem-details';

// Forms handle validation-failed themselves (errors keys = form control names);
// every other problem becomes one notification with the localized title and detail.
export const problemInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);
  return next(req.clone({ setHeaders: { 'Accept-Language': navigator.language } })).pipe(
    catchError((error: HttpErrorResponse) => {
      const problem = error.error;
      if (isProblem(problem) && problemCode(problem) !== 'validation-failed') {
        notifications.show({ title: problem.title, detail: problem.detail, traceId: problem.traceId });
      }
      return throwError(() => error);
    }),
  );
};
