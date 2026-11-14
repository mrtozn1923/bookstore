import { computed, effect, Service, signal } from '@angular/core';
import type { Book, CartItem } from '../models';

const storageKey = 'bookstore.cart';

// Global client state: one instance for the whole app (root injector)
@Service()
export class CartStore {
  readonly #items = signal<CartItem[]>(readSavedCart());

  // Read-only view of the state; every change goes through a method below
  readonly items = this.#items.asReadonly();
  readonly count = computed(() => this.#items().reduce((sum, item) => sum + item.quantity, 0));
  readonly totalPrice = computed(() =>
    this.#items().reduce((sum, item) => sum + item.price * item.quantity, 0),
  );

  constructor() {
    // Synchronise outwards only: write the cart to localStorage whenever it changes
    effect(() => localStorage.setItem(storageKey, JSON.stringify(this.#items())));
  }

  add(book: Book): void {
    this.#items.update((items) =>
      items.some((item) => item.isbn === book.isbn)
        ? items.map((item) =>
            item.isbn === book.isbn ? { ...item, quantity: item.quantity + 1 } : item,
          )
        : [...items, { isbn: book.isbn, title: book.title, price: book.price, quantity: 1 }],
    );
  }

  setQuantity(isbn: string, quantity: number): void {
    if (quantity < 1) {
      this.remove(isbn);
      return;
    }
    this.#items.update((items) =>
      items.map((item) => (item.isbn === isbn ? { ...item, quantity } : item)),
    );
  }

  remove(isbn: string): void {
    this.#items.update((items) => items.filter((item) => item.isbn !== isbn));
  }

  clear(): void {
    this.#items.set([]);
  }
}

function readSavedCart(): CartItem[] {
  try {
    const saved = localStorage.getItem(storageKey);
    return saved ? (JSON.parse(saved) as CartItem[]) : [];
  } catch {
    return []; // storage blocked or corrupt JSON: start with an empty cart
  }
}
