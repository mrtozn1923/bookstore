import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { BookstoreDatabase, PendingReview } from '../core/bookstore-database';

@Service()
export class ReviewOutbox {
  private readonly database = inject(BookstoreDatabase);
  private readonly http = inject(HttpClient);
  private flushing = false;
  readonly pending = signal(0);

  constructor() {
    window.addEventListener('online', () => void this.flush());
    void this.flush(); // something may have been left from the last visit
  }

  async enqueue(isbn: string, rating: number, comment: string) {
    const db = await this.database.connection();
    await db.put('outbox', {
      idempotencyKey: crypto.randomUUID(), // the same key on every attempt for this review
      isbn,
      rating,
      comment,
      createdAt: new Date().toISOString(),
    });
    await this.flush();
  }

  async flush() {
    if (this.flushing) return;
    this.flushing = true;
    try {
      const db = await this.database.connection();
      // read first, send afterwards: a transaction must not stay open while waiting for the network
      const items = await db.getAllFromIndex('outbox', 'byCreatedAt');
      this.pending.set(items.length);
      if (!navigator.onLine) return;
      for (const item of items) {
        if (!(await this.send(item))) break; // keep this and the later ones, in order
        await db.delete('outbox', item.idempotencyKey);
        this.pending.update((n) => n - 1);
      }
    } finally {
      this.flushing = false;
    }
  }

  // true when the item can leave the outbox
  private async send(item: PendingReview): Promise<boolean> {
    const key = item.idempotencyKey;
    try {
      const response = await firstValueFrom(
        this.http.post(
          `/api/books/${item.isbn}/reviews`,
          { rating: item.rating, comment: item.comment },
          { headers: { 'Idempotency-Key': key }, observe: 'response' },
        ),
      );
      const replayed = response.headers.has('Idempotency-Replayed') ? ' (replayed)' : '';
      console.log(`outbox: ${key.slice(0, 8)} -> ${response.status}${replayed}`);
      return true;
    } catch (err) {
      const status = err instanceof HttpErrorResponse ? err.status : 0;
      console.log(
        `outbox: ${key.slice(0, 8)} -> ${status === 0 ? 'network error, will retry' : status}`,
      );
      // a 4xx will not succeed later either, except timeouts and rate limiting
      return status >= 400 && status < 500 && status !== 408 && status !== 429;
    }
  }
}
