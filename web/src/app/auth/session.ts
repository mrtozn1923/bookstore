import { computed, effect, Injectable, signal } from '@angular/core';

const storageKey = 'bookstore.session';

export interface SessionUser {
  email: string;
  token: string;
}

@Injectable({ providedIn: 'root' })
export class Session {
  readonly user = signal<SessionUser | null>(JSON.parse(localStorage.getItem(storageKey) ?? 'null'));
  readonly signedIn = computed(() => this.user() !== null);

  constructor() {
    effect(() => {
      const user = this.user();
      if (user) {
        localStorage.setItem(storageKey, JSON.stringify(user));
      } else {
        localStorage.removeItem(storageKey);
      }
    });
  }
}
