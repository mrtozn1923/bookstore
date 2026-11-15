import { Injectable, computed, signal } from '@angular/core';
import type { BookDto } from '../api/book-api.service';

export interface CartLine {
  book: BookDto;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly linesSignal = signal<CartLine[]>([]);
  private toastTimer?: ReturnType<typeof setTimeout>;

  readonly lines = this.linesSignal.asReadonly();
  readonly count = computed(() => this.linesSignal().reduce((sum, line) => sum + line.quantity, 0));
  readonly subtotal = computed(() =>
    this.linesSignal().reduce((sum, line) => sum + line.book.price * line.quantity, 0),
  );
  readonly lastAdded = signal<string | null>(null);

  add(book: BookDto): void {
    this.linesSignal.update((lines) => {
      const existing = lines.find((line) => line.book.id === book.id);
      return existing
        ? lines.map((line) => (line === existing ? { ...line, quantity: line.quantity + 1 } : line))
        : [...lines, { book, quantity: 1 }];
    });
    this.lastAdded.set(book.title);
    clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.lastAdded.set(null), 3000);
  }

  clear(): void {
    this.linesSignal.set([]);
  }
}
