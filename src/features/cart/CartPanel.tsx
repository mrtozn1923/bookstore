import type { Dispatch } from 'react';
import { cartTotal, type CartAction } from '@/features/cart/cartReducer';
import type { CartLine } from '@/features/cart/types';
import { formatPrice } from '@/shared/format';

type CartPanelProps = {
  lines: CartLine[];
  dispatch: Dispatch<CartAction>;
};

export function CartPanel({ lines, dispatch }: CartPanelProps) {
  if (lines.length === 0) {
    return <aside className="cart-panel muted">Your cart is empty.</aside>;
  }

  return (
    <aside className="cart-panel">
      <h2>Cart</h2>
      <ul>
        {lines.map((line) => (
          <li key={line.isbn}>
            {line.title}{' '}
            <input type="number" min={0} max={99} aria-label={`Quantity of ${line.title}`} value={line.quantity}
              onChange={(e) => dispatch({ type: 'quantityChanged', isbn: line.isbn, quantity: e.target.valueAsNumber || 0 })} />{' '}
            {formatPrice(line.unitPrice * line.quantity)}{' '}
            <button type="button" onClick={() => dispatch({ type: 'removed', isbn: line.isbn })}>Remove</button>
          </li>
        ))}
      </ul>
      <p className="total">Total: {formatPrice(cartTotal(lines))}</p>
      <button type="button" onClick={() => dispatch({ type: 'cleared' })}>Clear cart</button>
    </aside>
  );
}
