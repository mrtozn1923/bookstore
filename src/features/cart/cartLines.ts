import type { Cart, CartLine } from './types';

export type QuantityChange = { isbn: string; title: string; unitPrice: number; quantity: number };

// Pure: our local guess of the cart after one quantity change (the server answer replaces it)
export function applyChange(cart: Cart, change: QuantityChange): Cart {
  const others = cart.items.filter((line) => line.isbn !== change.isbn);
  const items: CartLine[] = change.quantity > 0
    ? [...others, { ...change, lineTotal: change.unitPrice * change.quantity }].toSorted((a, b) => a.title.localeCompare(b.title))
    : others;
  return { ...cart, items, total: items.reduce((sum, line) => sum + line.lineTotal, 0) };
}
