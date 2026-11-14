import { effect, Injectable, signal } from '@angular/core';

const storageKey = 'bookstore.recentlyViewed';

@Injectable({ providedIn: 'root' })
export class RecentlyViewed {
  private readonly isbns = signal<readonly string[]>(JSON.parse(localStorage.getItem(storageKey) ?? '[]'));
  readonly list = this.isbns.asReadonly();

  constructor() {
    effect(() => localStorage.setItem(storageKey, JSON.stringify(this.isbns())));
  }

  mark(isbn: string): void {
    this.isbns.update((list) => [isbn, ...list.filter((item) => item !== isbn)].slice(0, 3));
  }
}
