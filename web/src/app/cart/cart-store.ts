import { computed, Injectable, signal } from '@angular/core';
import { Book } from '../catalog/book';

export interface CartLine {
  book: Book;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly items = signal<readonly CartLine[]>([]);
  readonly lines = this.items.asReadonly();
  readonly count = computed(() => this.items().reduce((count, line) => count + line.quantity, 0));
  readonly total = computed(() => this.items().reduce((sum, line) => sum + line.book.price * line.quantity, 0));

  add(book: Book): void {
    const existing = this.items().find((line) => line.book.isbn === book.isbn);
    if (existing) {
      this.setQuantity(book.isbn, existing.quantity + 1);
    } else {
      this.items.update((lines) => [...lines, { book, quantity: 1 }]);
    }
  }

  setQuantity(isbn: string, quantity: number): void {
    this.items.update((lines) =>
      lines.map((line) =>
        line.book.isbn === isbn ? { ...line, quantity: Math.min(Math.max(quantity, 1), line.book.stock) } : line,
      ),
    );
  }

  remove(isbn: string): void {
    this.items.update((lines) => lines.filter((line) => line.book.isbn !== isbn));
  }

  clear(): void {
    this.items.set([]);
  }
}
