import { computed, Service, signal } from '@angular/core';
import { Subject, type Observable } from 'rxjs';
import type { Book } from '../catalog/book';

export interface CartItem {
  book: Book;
  quantity: number;
}

@Service()
export class CartStore {
  // state: what the cart contains right now -> signal
  readonly #items = signal<CartItem[]>([]);
  readonly items = this.#items.asReadonly();
  readonly count = computed(() => this.#items().reduce((sum, i) => sum + i.quantity, 0));
  readonly total = computed(() => this.#items().reduce((sum, i) => sum + i.quantity * i.book.price, 0));

  // event: something just happened -> Subject, exposed read-only
  readonly #added = new Subject<Book>();
  readonly added$: Observable<Book> = this.#added.asObservable();

  add(book: Book): void {
    this.#items.update((items) => {
      const existing = items.find((i) => i.book.isbn === book.isbn);
      return existing
        ? items.map((i) => (i === existing ? { ...i, quantity: i.quantity + 1 } : i))
        : [...items, { book, quantity: 1 }];
    });
    this.#added.next(book);
  }
}
