import { createContext, use } from 'react';
import type { CartLine } from '@/features/cart/types';
import type { BookSummary } from '@/features/catalog/types';

export type CartContextValue = {
  lines: CartLine[];
  itemCount: number;
  total: number;
  add: (book: BookSummary, amount: number) => void;
  setQuantity: (isbn: string, quantity: number) => void;
  remove: (isbn: string) => void;
  restore: (line: CartLine) => void;
  clear: () => void;
};

// null default: reading the cart outside <CartProvider> is a bug, not a state
export const CartContext = createContext<CartContextValue | null>(null);

export function useCart(): CartContextValue {
  const cart = use(CartContext);
  if (cart === null) {
    throw new Error('useCart must be used inside <CartProvider>');
  }
  return cart;
}
