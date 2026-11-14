import { HttpErrorResponse } from '@angular/common/http';

// The body ASP.NET Core sends with application/problem+json (RFC 9457)
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

function isProblemDetails(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null && ('title' in body || 'detail' in body);
}

// a message the user can read, from whatever the HTTP call failed with
export function errorMessage(err: unknown): string {
  if (!(err instanceof HttpErrorResponse)) return 'Something went wrong.';
  if (err.status === 0) return 'Cannot reach the Bookstore service. Check your connection.';
  if (isProblemDetails(err.error)) {
    const validation = Object.values(err.error.errors ?? {}).flat();
    return validation.length ? validation.join(' ') : (err.error.detail ?? err.error.title ?? err.message);
  }
  return err.status >= 500 ? 'The Bookstore service is not responding. Please try again.' : err.message;
}
