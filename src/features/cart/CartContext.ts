import { createContext, use } from 'react';

// Only the cart's id is client state; the cart itself is server state in the query cache
export type CartIdContextValue = {
  cartId: string | null;
  setCartId: (cartId: string | null) => void;
};

export const CartIdContext = createContext<CartIdContextValue | null>(null);

export function useCartId(): CartIdContextValue {
  const value = use(CartIdContext);
  if (value === null) {
    throw new Error('useCartId must be used inside <CartProvider>');
  }
  return value;
}
