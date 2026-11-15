import { useState, type ReactNode } from 'react';
import { CartIdContext, type CartIdContextValue } from '@/features/cart/CartContext';

const storageKey = 'bookstore.cartId';

// Remembers which server-side cart belongs to this browser
export function CartProvider({ children }: { children: ReactNode }) {
  const [cartId, setCartIdState] = useState<string | null>(() => localStorage.getItem(storageKey));

  const value: CartIdContextValue = {
    cartId,
    setCartId: (id) => {
      if (id === null) {
        localStorage.removeItem(storageKey);
      } else {
        localStorage.setItem(storageKey, id);
      }
      setCartIdState(id);
    },
  };
  return <CartIdContext value={value}>{children}</CartIdContext>;
}
