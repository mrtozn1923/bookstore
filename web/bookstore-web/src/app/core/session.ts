import { httpResource } from '@angular/common/http';
import { computed, Injectable } from '@angular/core';
import { BffUser } from './models';

// Who is signed in. The BFF keeps the tokens; Angular only learns the name, e-mail and roles.
@Injectable({ providedIn: 'root' })
export class Session {
  // 401 means "no session": the resource ends in an error state and user() stays undefined
  private readonly userResource = httpResource<BffUser>(() => '/bff/user');

  readonly user = computed(() => (this.userResource.hasValue() ? this.userResource.value() : undefined));
  readonly isSignedIn = computed(() => this.user() !== undefined);
  readonly isLoading = this.userResource.isLoading;

  // A full page navigation: the OIDC flow runs in the BFF and Keycloak, not in Angular
  signIn(returnUrl: string = location.pathname): void {
    location.assign(`/bff/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  }

  // POST /bff/logout is a form post, so that the BFF can redirect the browser to Keycloak's logout page
  signOut(): void {
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/bff/logout';
    document.body.appendChild(form);
    form.submit();
  }
}
