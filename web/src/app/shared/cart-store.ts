import { Injectable, computed, signal } from '@angular/core';
import { Book } from '../catalog/book';

export interface CartLine {
  book: Book;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly items = signal<readonly CartLine[]>([]);

  readonly lines = this.items.asReadonly();
  readonly count = computed(() => this.items().reduce((sum, line) => sum + line.quantity, 0));
  readonly total = computed(() => this.items().reduce((sum, line) => sum + line.book.price * line.quantity, 0));

  add(book: Book): void {
    this.items.update((lines) => {
      const existing = lines.find((line) => line.book.isbn === book.isbn);
      return existing
        ? lines.map((line) => (line === existing ? { ...line, quantity: line.quantity + 1 } : line))
        : [...lines, { book, quantity: 1 }];
    });
  }
}
