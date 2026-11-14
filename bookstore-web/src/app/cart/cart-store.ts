import { computed, Service, signal } from '@angular/core';
import type { Book, CartItem } from '../models';

// Global client state: one instance for the whole app (root injector)
@Service()
export class CartStore {
  readonly #items = signal<CartItem[]>([]);
  readonly items = this.#items.asReadonly();
  readonly count = computed(() => this.#items().reduce((sum, item) => sum + item.quantity, 0));

  add(book: Book): void {
    this.#items.update((items) =>
      items.some((item) => item.isbn === book.isbn)
        ? items.map((item) => (item.isbn === book.isbn ? { ...item, quantity: item.quantity + 1 } : item))
        : [...items, { isbn: book.isbn, title: book.title, price: book.price, quantity: 1 }],
    );
  }
}
