import { inject, Service, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BookstoreDatabase } from './bookstore-database';

type SessionMessage = { type: 'signed-out' };

// No real sign-in in this series; the shape is what matters: sign-out reaches every tab.
@Service()
export class Session {
  private readonly router = inject(Router);
  private readonly database = inject(BookstoreDatabase);
  private readonly channel = new BroadcastChannel('bookstore-session');
  // '' after a sign-out; on the very first visit Ada is signed in
  readonly customer = signal(localStorage.getItem('customer') ?? 'ada');
  readonly signedOutElsewhere = signal(false);

  constructor() {
    this.channel.onmessage = ({ data }: MessageEvent<SessionMessage>) => {
      // a message can only say "sign out"; it can never sign anyone in or carry a token
      if (data?.type !== 'signed-out') return;
      console.log('session: signed out in another tab');
      void this.clear();
      this.signedOutElsewhere.set(true);
    };
  }

  async signOut() {
    await this.clear();
    this.channel.postMessage({ type: 'signed-out' } satisfies SessionMessage);
  }

  signIn(customer: string) {
    localStorage.setItem('customer', customer);
    this.customer.set(customer);
    this.signedOutElsewhere.set(false);
  }

  private async clear() {
    localStorage.setItem('customer', '');
    this.customer.set('');
    // personal data stays on a shared computer otherwise: the reading list and the review outbox
    const db = await this.database.connection();
    await Promise.all([db.clear('readingList'), db.clear('outbox')]);
    await this.router.navigateByUrl('/');
  }
}
