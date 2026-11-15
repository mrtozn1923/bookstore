import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router';
import type { CartLine } from './types';
import { useCart } from './useCart';
import { formatPrice } from '@/shared/format';

const undoMs = 5000;

export function CartPanel() {
  const { lines, total, error, setQuantity, remove, restore } = useCart();
  const [removed, setRemoved] = useState<CartLine | null>(null);
  // The timer id is not shown on screen: a ref keeps it without causing renders
  const undoTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  function stopUndoTimer() {
    if (undoTimer.current !== null) {
      clearTimeout(undoTimer.current);
      undoTimer.current = null;
    }
  }

  function handleRemove(line: CartLine) {
    remove(line);
    setRemoved(line);
    stopUndoTimer();
    undoTimer.current = setTimeout(() => setRemoved(null), undoMs);
  }

  function handleUndo(line: CartLine) {
    stopUndoTimer();
    restore(line);
    setRemoved(null);
  }

  // Do not leave a timer behind when the panel unmounts
  useEffect(() => stopUndoTimer, []);

  return (
    <section className="cart-panel">
      <h1>Cart</h1>
      {error && <p className="error" role="alert">Cart not updated: {error.message}</p>}
      {removed && (
        <p role="status">
          Removed {removed.title}.{' '}
          <button type="button" className="link" onClick={() => handleUndo(removed)}>Undo</button>
        </p>
      )}
      {lines.length === 0 ? (
        <p className="muted">Your cart is empty.</p>
      ) : (
        <>
          <ul>
            {lines.map((line) => (
              <li key={line.isbn}>
                {line.title}{' '}
                <input type="number" min={0} max={99} aria-label={`Quantity of ${line.title}`} value={line.quantity}
                  onChange={(e) => setQuantity({ ...line, quantity: e.target.valueAsNumber || 0 })} />{' '}
                {formatPrice(line.lineTotal)}{' '}
                <button type="button" onClick={() => handleRemove(line)}>Remove</button>
              </li>
            ))}
          </ul>
          <p className="total">Total: {formatPrice(total)}</p>
          <Link to="/checkout" className="button">Checkout</Link>
        </>
      )}
    </section>
  );
}
