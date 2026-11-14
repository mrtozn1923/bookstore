import { computed, effect, Injectable, signal } from '@angular/core';

export interface CartLine {
  isbn: string;
  title: string;
  unitPrice: number;
  currency: string;
  quantity: number;
}

const storageKey = 'bookstore.cart';

// The cart lives only in the browser: no service owns it until the customer places the order.
@Injectable({ providedIn: 'root' })
export class Cart {
  readonly lines = signal<CartLine[]>(read());
  readonly count = computed(() => this.lines().reduce((sum, line) => sum + line.quantity, 0));
  readonly total = computed(() => this.lines().reduce((sum, line) => sum + line.unitPrice * line.quantity, 0));

  constructor() {
    // Every change is written to localStorage, so the cart survives a reload and the OIDC redirect
    effect(() => localStorage.setItem(storageKey, JSON.stringify(this.lines())));
  }

  add(line: Omit<CartLine, 'quantity'>, quantity = 1): void {
    this.lines.update(lines =>
      lines.some(l => l.isbn === line.isbn)
        ? lines.map(l => (l.isbn === line.isbn ? { ...l, quantity: l.quantity + quantity } : l))
        : [...lines, { ...line, quantity }]
    );
  }

  setQuantity(isbn: string, quantity: number): void {
    this.lines.update(lines =>
      quantity <= 0 ? lines.filter(l => l.isbn !== isbn) : lines.map(l => (l.isbn === isbn ? { ...l, quantity } : l))
    );
  }

  // Ordering answered 409 "Prices changed": the customer sees the new price before ordering again
  applyPrices(changes: { isbn: string; currentPrice: number }[]): void {
    this.lines.update(lines =>
      lines.map(l => {
        const change = changes.find(c => c.isbn === l.isbn);
        return change ? { ...l, unitPrice: change.currentPrice } : l;
      })
    );
  }

  clear(): void {
    this.lines.set([]);
  }
}

function read(): CartLine[] {
  try {
    return JSON.parse(localStorage.getItem(storageKey) ?? '[]') as CartLine[];
  } catch {
    return [];
  }
}
