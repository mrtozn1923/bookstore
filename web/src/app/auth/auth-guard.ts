import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Session } from './session';

export const authGuard: CanActivateFn = (_route, state) =>
  inject(Session).signedIn() || inject(Router).createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } });
