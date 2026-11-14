import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn()
    ? true
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const authMatchGuard: CanMatchFn = () =>
  inject(AuthService).isLoggedIn() || inject(Router).createUrlTree(['/login']);

export function roleGuard(role: string): CanActivateFn {
  return () => inject(AuthService).hasRole(role);
}
