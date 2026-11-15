import { useReducer, type ReactNode } from 'react';
import { CartContext, type CartContextValue } from '@/features/cart/CartContext';
import { cartItemCount, cartReducer, cartTotal } from '@/features/cart/cartReducer';

// Owns the cart state; every component below can read it with useCart()
export function CartProvider({ children }: { children: ReactNode }) {
  const [lines, dispatch] = useReducer(cartReducer, []);

  const value: CartContextValue = {
    lines,
    itemCount: cartItemCount(lines),
    total: cartTotal(lines),
    add: (book, amount) => dispatch({ type: 'added', book, amount }),
    setQuantity: (isbn, quantity) => dispatch({ type: 'quantityChanged', isbn, quantity }),
    remove: (isbn) => dispatch({ type: 'removed', isbn }),
    restore: (line) => dispatch({ type: 'restored', line }),
    clear: () => dispatch({ type: 'cleared' }),
  };

  return <CartContext value={value}>{children}</CartContext>;
}
