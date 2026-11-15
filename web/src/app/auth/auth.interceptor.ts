import { HttpInterceptorFn } from '@angular/common/http';

export const accessTokenKey = 'bookstore.accessToken';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = localStorage.getItem(accessTokenKey);
  if (!token || !request.url.startsWith('/api/')) {
    return next(request);
  }
  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
