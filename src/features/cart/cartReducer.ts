import type { CartLine } from '@/features/cart/types';
import type { BookSummary } from '@/features/catalog/types';

// Every way the cart can change, as a discriminated union on "type"
export type CartAction =
  | { type: 'added'; book: BookSummary; amount: number }
  | { type: 'quantityChanged'; isbn: string; quantity: number }
  | { type: 'removed'; isbn: string }
  | { type: 'restored'; line: CartLine }
  | { type: 'cleared' };

// A pure function: same (state, action) always gives the same new state; no mutation, no I/O
export function cartReducer(lines: CartLine[], action: CartAction): CartLine[] {
  switch (action.type) {
    case 'added': {
      const { book, amount } = action;
      return lines.some((line) => line.isbn === book.isbn)
        ? lines.map((line) => (line.isbn === book.isbn ? { ...line, quantity: line.quantity + amount } : line))
        : [...lines, { isbn: book.isbn, title: book.title, unitPrice: book.price, quantity: amount }];
    }
    case 'quantityChanged':
      return action.quantity <= 0
        ? lines.filter((line) => line.isbn !== action.isbn)
        : lines.map((line) => (line.isbn === action.isbn ? { ...line, quantity: action.quantity } : line));
    case 'removed':
      return lines.filter((line) => line.isbn !== action.isbn);
    case 'restored':
      return lines.some((line) => line.isbn === action.line.isbn) ? lines : [...lines, action.line];
    case 'cleared':
      return [];
    default: {
      // If a new action type is added and not handled above, this line stops compiling
      const unhandled: never = action;
      throw new Error(`Unknown cart action: ${JSON.stringify(unhandled)}`);
    }
  }
}

export function cartItemCount(lines: CartLine[]): number {
  return lines.reduce((sum, line) => sum + line.quantity, 0);
}

export function cartTotal(lines: CartLine[]): number {
  return lines.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0);
}
