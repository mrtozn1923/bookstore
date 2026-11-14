import { HttpClient } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { email, form, FormField, FormRoot, required } from '@angular/forms/signals';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Session, SessionUser } from './session';

@Component({
  selector: 'app-sign-in-page',
  imports: [FormField, FormRoot],
  template: `
    <h1>Sign in</h1>
    <form [formRoot]="signInForm">
      <label for="signInEmail">Email</label>
      <input id="signInEmail" type="email" autocomplete="username" [formField]="signInForm.email" />
      <label for="signInPassword">Password</label>
      <input id="signInPassword" type="password" autocomplete="current-password" [formField]="signInForm.password" />
      @if (failed()) {
        <p role="alert">Email or password is wrong.</p>
      }
      <button type="submit">Sign in</button>
    </form>
  `,
})
export class SignInPage {
  readonly returnUrl = input('/');

  private readonly http = inject(HttpClient);
  private readonly session = inject(Session);
  private readonly router = inject(Router);
  protected readonly failed = signal(false);

  private readonly credentials = signal({ email: '', password: '' });
  protected readonly signInForm = form(
    this.credentials,
    (path) => {
      required(path.email);
      email(path.email);
      required(path.password);
    },
    {
      submission: {
        action: async (field) => {
          try {
            const user = await firstValueFrom(this.http.post<SessionUser>('/api/auth/sign-in', field().value()));
            this.session.user.set(user);
            await this.router.navigateByUrl(this.returnUrl());
          } catch {
            this.failed.set(true);
          }
        },
      },
    },
  );
}
