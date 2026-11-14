import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthService {
  get token(): string | null {
    return localStorage.getItem('token');
  }

  isLoggedIn(): boolean {
    return this.token !== null;
  }

  hasRole(role: string): boolean {
    return localStorage.getItem('role') === role;
  }
}
