import { DOCUMENT, InjectionToken, inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';

// Browser: the page's own origin. Prerendering (app.config.server.ts): the API's address at build time.
// Both sides send the same absolute URL, so the HTTP transfer cache finds the server's response.
export const API_ORIGIN = new InjectionToken<string>('API_ORIGIN', {
  factory: () => inject(DOCUMENT).location.origin,
});

export const apiOriginInterceptor: HttpInterceptorFn = (req, next) =>
  req.url.startsWith('/api/') ? next(req.clone({ url: inject(API_ORIGIN) + req.url })) : next(req);
