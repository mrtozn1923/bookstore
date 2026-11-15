import { useEffect, useRef, useState } from 'react';
import { useCart } from '@/features/cart/CartContext';
import type { CartLine } from '@/features/cart/types';
import { formatPrice } from '@/shared/format';

const undoMs = 5000;

export function CartPanel() {
  const { lines, total, setQuantity, remove, restore, clear } = useCart();
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
    remove(line.isbn);
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
    <aside className="cart-panel">
      <h2>Cart</h2>
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
                  onChange={(e) => setQuantity(line.isbn, e.target.valueAsNumber || 0)} />{' '}
                {formatPrice(line.unitPrice * line.quantity)}{' '}
                <button type="button" onClick={() => handleRemove(line)}>Remove</button>
              </li>
            ))}
          </ul>
          <p className="total">Total: {formatPrice(total)}</p>
          <button type="button" onClick={clear}>Clear cart</button>
        </>
      )}
    </aside>
  );
}
