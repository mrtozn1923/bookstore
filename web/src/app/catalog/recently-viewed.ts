import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class RecentlyViewed {
  private readonly isbns = signal<readonly string[]>([]);
  readonly list = this.isbns.asReadonly();

  mark(isbn: string): void {
    this.isbns.update((list) => [isbn, ...list.filter((item) => item !== isbn)].slice(0, 3));
  }
}
